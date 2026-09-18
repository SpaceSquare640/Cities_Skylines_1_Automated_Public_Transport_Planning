using ColossalFramework;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// Works out where a stop may legally sit on a road segment.
    ///
    /// The game does this in TransportTool.GetStopPosition, which is private, so the
    /// same steps are reproduced here from the public pieces it calls. Keeping our
    /// own copy avoids reflecting into a private method whose signature could change,
    /// at the cost of having to track the original if the game's rules ever move.
    /// </summary>
    public static class StopPlacement
    {
        /// <summary>
        /// Lane offset the game uses when placing a stop: the midpoint of the lane,
        /// as seen in TransportTool.
        /// </summary>
        private const float LaneMidpoint = 0.5019608f;

        /// <summary>
        /// How far PathManager is allowed to look for a usable lane around a stop.
        /// Matches the radius TransportLineAI.StartPathFind passes.
        /// </summary>
        public const float PathSearchRadius = 32f;

        /// <summary>
        /// Computes the stop position for a segment, or returns false when the
        /// segment cannot host a stop for this transport type.
        ///
        /// The original has two paths and they produce different positions:
        ///
        ///   m_vehicleType == None  (pedestrian lines) — the stop sits on the
        ///     pedestrian lane itself.
        ///   m_vehicleType != None  (bus, tram, ...)   — the pedestrian lane is only
        ///     used to find the spot; a second search then picks the vehicle lane,
        ///     and the stop sits on that.
        ///
        /// A bus prefab carries m_vehicleType = Car (confirmed in-game), so buses take
        /// the second path. Placing a bus stop on the pedestrian lane instead puts it
        /// roughly a lane width away from where the game would, which is close enough
        /// for the path finder's 32m search to usually recover — and therefore fails
        /// intermittently rather than outright.
        ///
        /// IMPORTANT — the returned position is NOT free to slide along the segment.
        /// Both branches of TransportTool.GetStopPosition pass a hardcoded lane offset
        /// of 0.5019608 (TransportTool lines 1110 and 1129), and
        /// NetLane.CalculateStopPositionAndDirection evaluates the lane bezier at it.
        /// So a segment offers exactly one stop position per lane: the lane's midpoint.
        ///
        /// That is why the hint parameter is called sideHint and not "near": it only
        /// decides WHICH lane is chosen, never where along the segment the stop lands.
        /// Passing a building entrance and expecting the stop to appear next to it
        /// silently yields the segment midpoint instead — over 50m out on a long rural
        /// segment. Callers that need finer spacing must pick a different segment.
        ///
        /// Known defect S13: a centreline point is equidistant from both sidewalks, and
        /// NetSegment.GetClosestLanePosition breaks that tie with a strict "&lt;" while
        /// walking m_lanes in declaration order — so the side, and with it the travel
        /// direction, is effectively arbitrary. Callers that care must pass a sideHint
        /// that is genuinely offset towards the side they want. Resolving this properly
        /// needs the corridor-direction concept that does not exist yet.
        /// </summary>
        public static bool TryGetStopPosition(ushort segmentId, Vector3 sideHint, TransportInfo info,
                                              out Vector3 stopPosition, out bool fixedPlatform)
        {
            stopPosition = Vector3.zero;
            fixedPlatform = false;

            if (segmentId == 0 || info == null)
            {
                return false;
            }

            // The original wraps its whole segment branch in this test
            // (TransportTool line 1057) and sends pedestrian lines to the building
            // branch instead, which this class does not implement. Without the gate a
            // walking-tour line would be handed a sidewalk position the game would
            // never have offered.
            if (info.m_transportType == TransportInfo.TransportType.Pedestrian)
            {
                return false;
            }

            NetManager net = Singleton<NetManager>.instance;

            if ((net.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
            {
                return false;
            }

            NetInfo segmentInfo = net.m_segments.m_buffer[segmentId].Info;
            if (segmentInfo == null || segmentInfo.m_lanes == null)
            {
                return false;
            }

            // Step one: find the pedestrian lane. Its position anchors the search
            // that follows, and its flags decide whether another kind of stop is
            // already sitting here.
            Vector3 pedestrianPosition;
            uint pedestrianLaneId;
            int pedestrianLaneIndex;
            float pedestrianOffset;

            if (!net.m_segments.m_buffer[segmentId].GetClosestLanePosition(
                    sideHint,
                    NetInfo.LaneType.Pedestrian,
                    VehicleInfo.VehicleType.None,
                    info.vehicleCategory,
                    info.m_vehicleType,
                    out pedestrianPosition,
                    out pedestrianLaneId,
                    out pedestrianLaneIndex,
                    out pedestrianOffset))
            {
                return false;
            }

            if (info.m_vehicleType == VehicleInfo.VehicleType.None)
            {
                return TryPedestrianStop(segmentId, segmentInfo, info,
                                         pedestrianLaneId, pedestrianLaneIndex,
                                         out stopPosition, out fixedPlatform);
            }

            return TryVehicleStop(segmentId, segmentInfo, info,
                                  pedestrianPosition, pedestrianLaneId,
                                  out stopPosition, out fixedPlatform);
        }

        /// <summary>
        /// Pedestrian lines: the stop sits on the pedestrian lane. Only this path
        /// clears the wanted stop flag for dedicated networks.
        /// </summary>
        private static bool TryPedestrianStop(ushort segmentId, NetInfo segmentInfo, TransportInfo info,
                                              uint pedestrianLaneId, int pedestrianLaneIndex,
                                              out Vector3 stopPosition, out bool fixedPlatform)
        {
            stopPosition = Vector3.zero;
            fixedPlatform = false;

            NetManager net = Singleton<NetManager>.instance;

            NetLane.Flags laneStopFlags = (NetLane.Flags)net.m_lanes.m_buffer[pedestrianLaneId].m_flags & NetLane.Flags.Stops;
            NetLane.Flags wantedStopFlag = info.m_stopFlag;

            // Dedicated networks (tracks, canals) do not use the road stop flags.
            if (segmentInfo.m_vehicleTypes != VehicleInfo.VehicleType.None)
            {
                wantedStopFlag = NetLane.Flags.None;
            }

            if (laneStopFlags != NetLane.Flags.None &&
                wantedStopFlag != NetLane.Flags.None &&
                laneStopFlags != wantedStopFlag)
            {
                return false;
            }

            float stopOffset = segmentInfo.m_lanes[pedestrianLaneIndex].m_stopOffset;
            if ((net.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None)
            {
                stopOffset = -stopOffset;
            }

            Vector3 direction;
            net.m_lanes.m_buffer[pedestrianLaneId].CalculateStopPositionAndDirection(
                LaneMidpoint, stopOffset, out stopPosition, out direction);

            fixedPlatform = true;
            return true;
        }

        /// <summary>
        /// Vehicle lines: a second search, anchored on the pedestrian lane position,
        /// picks the lane the vehicle will stop in. Note that the stop flags are still
        /// read from the pedestrian lane, and that there is no clearing of the wanted
        /// flag here — both match the original.
        /// </summary>
        private static bool TryVehicleStop(ushort segmentId, NetInfo segmentInfo, TransportInfo info,
                                           Vector3 pedestrianPosition, uint pedestrianLaneId,
                                           out Vector3 stopPosition, out bool fixedPlatform)
        {
            stopPosition = Vector3.zero;
            fixedPlatform = false;

            NetManager net = Singleton<NetManager>.instance;

            Vector3 vehiclePosition;
            uint vehicleLaneId;
            int vehicleLaneIndex;
            float vehicleOffset;

            if (!net.m_segments.m_buffer[segmentId].GetClosestLanePosition(
                    pedestrianPosition,
                    NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle,
                    info.m_vehicleType,
                    info.vehicleCategory,
                    out vehiclePosition,
                    out vehicleLaneId,
                    out vehicleLaneIndex,
                    out vehicleOffset))
            {
                return false;
            }

            if (vehicleLaneIndex < 0 || vehicleLaneIndex >= segmentInfo.m_lanes.Length)
            {
                return false;
            }

            // Stop flags come from the pedestrian lane even on this path.
            NetLane.Flags laneStopFlags = (NetLane.Flags)net.m_lanes.m_buffer[pedestrianLaneId].m_flags & NetLane.Flags.Stops;
            if (laneStopFlags != NetLane.Flags.None &&
                info.m_stopFlag != NetLane.Flags.None &&
                laneStopFlags != info.m_stopFlag)
            {
                return false;
            }

            float stopOffset = segmentInfo.m_lanes[vehicleLaneIndex].m_stopOffset;
            if ((net.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None)
            {
                stopOffset = -stopOffset;
            }

            Vector3 direction;
            net.m_lanes.m_buffer[vehicleLaneId].CalculateStopPositionAndDirection(
                LaneMidpoint, stopOffset, out stopPosition, out direction);

            fixedPlatform = true;
            return true;
        }

        /// <summary>
        /// Whether the path manager can anchor a stop at this position at all: is
        /// there a lane within the search radius that a stop could attach to. This is
        /// the same question TransportLineAI.StartPathFind asks for each end of a
        /// line segment, so a position that fails here will certainly fail there.
        ///
        /// What it does NOT answer is whether a bus can drive from one stop to the
        /// next. That is a property of a pair of stops, not of a single one, and only
        /// the asynchronous path find can settle it — which is why the validation
        /// loop checks PathFailed after the stops exist, rather than trying to rule
        /// every problem out in advance.
        ///
        /// Note on the two out positions: pathPosA and pathPosB are the closest
        /// forward-direction and backward-direction lanes, not a pedestrian and a
        /// vehicle position. pathPosB is legitimately zero on a one-way street, so
        /// testing it rejects perfectly good stops.
        ///
        /// Known defect S15 — this test is LOOSER than the one that actually decides
        /// whether a stop works. TransportLineAI.UpdateLaneConnection repeats the
        /// search with requireConnect = true and the line's real vehicle category, and
        /// for a vehicle line additionally demands that NetSegment.GetClosestLane
        /// succeed. If any of that fails, NetNode.m_lane stays zero, AddLaneConnection
        /// never runs, and the stop silently has no lane to serve it. Passing here is
        /// therefore necessary but not sufficient: confirm with HasLaneConnection once
        /// the stops exist.
        /// </summary>
        public static bool CanAnchorStop(Vector3 position, TransportInfo info)
        {
            if (info == null)
            {
                return false;
            }

            PathUnit.Position forwardPos;
            PathUnit.Position backwardPos;
            float forwardDistance;
            float backwardDistance;

            return PathManager.FindPathPosition(
                position,
                info.m_netService,
                info.m_secondaryNetService,
                NetInfo.LaneType.Pedestrian,
                VehicleInfo.VehicleType.None,
                VehicleInfo.VehicleCategory.None,
                info.m_vehicleType,
                true,
                false,
                PathSearchRadius,
                false,
                true,
                out forwardPos,
                out backwardPos,
                out forwardDistance,
                out backwardDistance);
        }

        /// <summary>
        /// Whether the game has actually bound a lane to this stop node.
        ///
        /// TransportLineAI.UpdateLaneConnection writes NetNode.m_lane and then
        /// AddLaneConnection sets the Stop flag on that lane. A node whose m_lane is
        /// still zero is a dead stop: it looks fine, reports no problem flag, and no
        /// vehicle can ever serve it. This is the only reliable way to tell.
        ///
        /// UpdateLaneConnection runs asynchronously, so give the simulation a few
        /// frames after creating the stops before trusting a false here.
        /// </summary>
        public static bool HasLaneConnection(ushort nodeId)
        {
            if (nodeId == 0)
            {
                return false;
            }

            NetManager net = Singleton<NetManager>.instance;

            if ((net.m_nodes.m_buffer[nodeId].m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
            {
                return false;
            }

            return net.m_nodes.m_buffer[nodeId].m_lane != 0U;
        }

        /// <summary>
        /// Whether a segment is a road this transport type could serve.
        ///
        /// The vehicle test is on m_vehicleCategories rather than m_vehicleTypes: the
        /// latter only goes as far as "Car", which cannot tell a road that allows
        /// buses from one that does not. NetInfo.InitializePrefab builds
        /// m_vehicleCategories by unioning the categories of every Vehicle or
        /// TransportVehicle lane whose m_vehicleType is not None, and
        /// NetInfo.Lane.CheckType is what the game itself tests a lane against when
        /// picking one — so this is a cheap segment-level prefilter for the same rule.
        ///
        /// It is deliberately NOT the check PublicTransportPanel does. That one decides
        /// which road prefabs appear under the toolbar's public-transport tab, and it
        /// excludes roads that also allow trams — roads a bus stop must accept.
        ///
        /// The excluded flags are the same ones PathManager.FindPathPosition skips, so
        /// ruling them out here avoids offering candidates that could never anchor.
        /// </summary>
        public static bool IsSegmentUsableForStops(ushort segmentId, TransportInfo info)
        {
            if (segmentId == 0 || info == null)
            {
                return false;
            }

            NetManager net = Singleton<NetManager>.instance;

            NetSegment.Flags flags = net.m_segments.m_buffer[segmentId].m_flags;

            if ((flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
            {
                return false;
            }

            // Untouchable segments belong to a station building and take a different
            // path in the original (FindOwnerBuilding), which this does not implement.
            if ((flags & (NetSegment.Flags.Collapsed | NetSegment.Flags.Flooded | NetSegment.Flags.Untouchable)) != NetSegment.Flags.None)
            {
                return false;
            }

            if ((net.m_segments.m_buffer[segmentId].m_flags2 & NetSegment.Flags2.EventClosed) != NetSegment.Flags2.None)
            {
                return false;
            }

            NetInfo segmentInfo = net.m_segments.m_buffer[segmentId].Info;
            if (segmentInfo == null)
            {
                return false;
            }

            if (segmentInfo.m_netAI != null && segmentInfo.m_netAI.IsUnderground())
            {
                return false;
            }

            // Passengers need somewhere to stand.
            if ((segmentInfo.m_laneTypes & NetInfo.LaneType.Pedestrian) == NetInfo.LaneType.None)
            {
                return false;
            }

            // And the vehicle needs a lane it is allowed to use.
            if ((segmentInfo.m_laneTypes & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) == NetInfo.LaneType.None)
            {
                return false;
            }

            if ((segmentInfo.m_vehicleCategories & info.vehicleCategory) == VehicleInfo.VehicleCategory.None)
            {
                return false;
            }

            return true;
        }
    }
}
