using ColossalFramework;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// Works out where a stop may legally sit on a road segment.
    ///
    /// The game does this in TransportTool.GetStopPosition, which is private, so the
    /// same steps are reproduced here from the public pieces it calls: snap to the
    /// segment's pedestrian lane, then let the lane geometry produce the exact stop
    /// position. Keeping our own copy avoids reflecting into a private method whose
    /// signature could change, at the cost of having to track the original if the
    /// game's rules ever move.
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
        /// </summary>
        public static bool TryGetStopPosition(ushort segmentId, Vector3 near, TransportInfo info,
                                              out Vector3 stopPosition, out bool fixedPlatform)
        {
            stopPosition = Vector3.zero;
            fixedPlatform = false;

            if (segmentId == 0 || info == null)
            {
                return false;
            }

            NetManager net = Singleton<NetManager>.instance;
            NetSegment segment = net.m_segments.m_buffer[segmentId];

            if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
            {
                return false;
            }

            NetInfo segmentInfo = segment.Info;
            if (segmentInfo == null || segmentInfo.m_lanes == null)
            {
                return false;
            }

            Vector3 lanePosition;
            uint laneId;
            int laneIndex;
            float laneOffset;

            bool found = net.m_segments.m_buffer[segmentId].GetClosestLanePosition(
                near,
                NetInfo.LaneType.Pedestrian,
                VehicleInfo.VehicleType.None,
                info.vehicleCategory,
                info.m_vehicleType,
                out lanePosition,
                out laneId,
                out laneIndex,
                out laneOffset);

            if (!found || laneIndex < 0 || laneIndex >= segmentInfo.m_lanes.Length)
            {
                return false;
            }

            // A lane that already carries a different kind of stop cannot take ours.
            NetLane.Flags laneStopFlags = (NetLane.Flags)net.m_lanes.m_buffer[laneId].m_flags & NetLane.Flags.Stops;
            NetLane.Flags wantedStopFlag = info.m_stopFlag;
            if (segmentInfo.m_vehicleTypes != VehicleInfo.VehicleType.None)
            {
                // Dedicated networks (tracks, canals) do not use the road stop flags.
                wantedStopFlag = NetLane.Flags.None;
            }

            if (laneStopFlags != NetLane.Flags.None &&
                wantedStopFlag != NetLane.Flags.None &&
                laneStopFlags != wantedStopFlag)
            {
                return false;
            }

            float stopOffset = segmentInfo.m_lanes[laneIndex].m_stopOffset;
            if ((segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None)
            {
                stopOffset = -stopOffset;
            }

            Vector3 direction;
            net.m_lanes.m_buffer[laneId].CalculateStopPositionAndDirection(
                LaneMidpoint, stopOffset, out stopPosition, out direction);

            fixedPlatform = true;
            return true;
        }

        /// <summary>
        /// Asks the path manager whether a vehicle of this type could actually reach
        /// the position. A stop that fails this will leave the line marked as not
        /// connected once the asynchronous path find catches up, so it is worth
        /// checking before the stop is ever created.
        ///
        /// FindPathPosition returns two positions: a pedestrian one and a vehicle
        /// one. Its boolean result only tells us that the pedestrian side was found,
        /// which is why an earlier version of this check passed every stop and the
        /// line still came back with failed paths. The vehicle position is the part
        /// that decides whether a bus can get there, so it is what we test.
        /// </summary>
        public static bool IsReachable(Vector3 position, TransportInfo info)
        {
            if (info == null)
            {
                return false;
            }

            PathUnit.Position pedestrianPos;
            PathUnit.Position vehiclePos;
            float pedestrianDistance;
            float vehicleDistance;

            bool found = PathManager.FindPathPosition(
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
                out pedestrianPos,
                out vehiclePos,
                out pedestrianDistance,
                out vehicleDistance);

            // A zero segment on the vehicle position means nothing drivable was found
            // within the search radius, however good the pedestrian side looked.
            return found && vehiclePos.m_segment != 0;
        }

        /// <summary>
        /// Whether a segment is a road a bus could serve: it needs a lane the vehicle
        /// can drive on as well as the pedestrian lane the stop itself attaches to.
        ///
        /// Filtering on the pedestrian lane alone lets footpaths, park paths and
        /// pedestrian streets through — they carry pedestrian lanes and no car lane
        /// at all, so stops placed on them can never be served.
        /// </summary>
        public static bool IsSegmentUsableForStops(ushort segmentId, TransportInfo info)
        {
            if (segmentId == 0 || info == null)
            {
                return false;
            }

            NetManager net = Singleton<NetManager>.instance;

            if ((net.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
            {
                return false;
            }

            NetInfo segmentInfo = net.m_segments.m_buffer[segmentId].Info;
            if (segmentInfo == null)
            {
                return false;
            }

            // The stop needs somewhere for passengers to stand.
            if ((segmentInfo.m_laneTypes & NetInfo.LaneType.Pedestrian) == NetInfo.LaneType.None)
            {
                return false;
            }

            // And the vehicle needs somewhere to drive.
            if ((segmentInfo.m_laneTypes & NetInfo.LaneType.Vehicle) == NetInfo.LaneType.None &&
                (segmentInfo.m_laneTypes & NetInfo.LaneType.TransportVehicle) == NetInfo.LaneType.None)
            {
                return false;
            }

            if ((segmentInfo.m_vehicleTypes & info.m_vehicleType) == VehicleInfo.VehicleType.None)
            {
                return false;
            }

            return true;
        }
    }
}
