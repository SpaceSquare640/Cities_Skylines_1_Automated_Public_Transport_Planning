using System;
using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using UnityEngine;
using AutomatedPublicTransportPlanning.Planning;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Spike
{
    /// <summary>
    /// Spike 1: prove that a line this mod creates is an ordinary vanilla line —
    /// that it survives a save/load round trip intact, and that the save still opens
    /// with the mod removed.
    ///
    /// Everything here is throwaway diagnostic code. It writes to the player's city,
    /// so every line it makes is named with a recognisable prefix and can be removed
    /// again from the same panel.
    ///
    /// All of it runs on the simulation thread. Scanning segments and lanes from the
    /// UI thread reads structures the simulation is concurrently rewriting: the grid
    /// and lane lists can be re-linked mid-walk, and a sort whose comparer re-reads
    /// moving positions throws once the ordering stops being consistent. The whole
    /// survey measures in single-digit milliseconds, so there is nothing to gain by
    /// doing it anywhere else.
    /// </summary>
    public static class SaveRoundTripSpike
    {
        /// <summary>Prefix that marks a line as belonging to this spike.</summary>
        public const string TestLinePrefix = "APTP Test";

        private const int MaxStops = 6;
        private const int MinStops = 3;

        /// <summary>
        /// Wide enough that the test line has a real shape. A tight cluster of stops
        /// barely exercises the line length or the path finding between them.
        /// </summary>
        private const float MinStopSpacing = 400f;

        /// <summary>
        /// AddStop closes the line instead of appending when the new position is
        /// within 2.5m of the first stop (it compares squared distance against 6.25).
        /// </summary>
        private const float LoopClosingDistance = 2.5f;

        public static void CreateTestLine()
        {
            if (!Singleton<TransportManager>.exists || !Singleton<NetManager>.exists ||
                !Singleton<SimulationManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to do.");
                return;
            }

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    BuildTestLine();
                }
                catch (Exception e)
                {
                    Log.Exception("CreateTestLine failed", e);
                }
            });
        }

        private static void BuildTestLine()
        {
            TransportInfo busInfo = BusPrefabResolver.ResolveCityBus();
            if (busInfo == null)
            {
                Log.Error("Cannot create a test line: no city bus prefab in this save.");
                return;
            }

            NetManager net = Singleton<NetManager>.instance;
            TransportManager tm = Singleton<TransportManager>.instance;

            if (!net.CheckLimits() || !tm.CheckLimits())
            {
                Log.Error("The game is at its network or transport line limit; refusing to build.");
                return;
            }

            List<StopSite> sites = PickStopSites(busInfo);
            if (sites.Count < MinStops)
            {
                Log.Error("Only found " + sites.Count + " usable stop positions; need at least " + MinStops + ".");
                return;
            }

            ushort lineId;
            if (!tm.CreateLine(out lineId, ref Singleton<SimulationManager>.instance.m_randomizer, busInfo, true))
            {
                Log.Error("CreateLine returned false. The line limit may have been reached.");
                return;
            }

            Log.Info("Created line " + lineId + " with " + sites.Count + " stops to add.");

            for (int i = 0; i < sites.Count; i++)
            {
                // index -1 appends to the end of the line.
                if (!tm.m_lines.m_buffer[lineId].AddStop(lineId, -1, sites[i].Position, sites[i].FixedPlatform))
                {
                    Log.Error("AddStop rejected stop " + i + " at " + sites[i].Position +
                              " (segment " + sites[i].SegmentId + "). Rolling the line back.");
                    tm.ReleaseLine(lineId);
                    return;
                }
            }

            // A line only starts running vehicles once it is Complete, and AddStop only
            // sets that flag when a stop lands on the first one, closing the loop. Without
            // this the line looks finished, reports a target vehicle count, and never
            // moves a single bus.
            if (!CloseLoop(lineId, sites[0]))
            {
                Log.Error("Could not close the line into a loop. Rolling it back.");
                tm.ReleaseLine(lineId);
                return;
            }

            // The name is the only handle that survives a save/load round trip, so it
            // is also the only way the report and remove buttons can find this line
            // again. A rename that quietly failed would leave the player with a line
            // this mod can no longer clean up, which breaks the one-click rollback rule
            // — so it is treated as a build failure.
            string name = TestLinePrefix + " " + DateTime.Now.ToString("HH:mm:ss");
            bool renamed = false;
            IEnumerator<bool> rename = tm.SetLineName(lineId, name);
            while (rename.MoveNext())
            {
                // SetLineName does its work before it yields; the last value it yields
                // is the success flag.
                renamed = rename.Current;
            }

            if (!renamed)
            {
                Log.Error("SetLineName failed for line " + lineId +
                          ". Rolling it back rather than leaving a line the remove button cannot find.");
                tm.ReleaseLine(lineId);
                return;
            }

            tm.UpdateLine(lineId);

            Log.Info("Line " + lineId + " named '" + name + "' is complete with " +
                     tm.m_lines.m_buffer[lineId].CountStops(lineId) + " stops. " +
                     "Save, reload, then use the report button to compare.");
        }

        /// <summary>
        /// Adds a stop on top of the first one, which AddStop treats as a request to
        /// close the line rather than to append. The closing branch can still fail if
        /// the game is out of segments, so the flag is checked rather than assumed.
        /// </summary>
        private static bool CloseLoop(ushort lineId, StopSite firstSite)
        {
            TransportManager tm = Singleton<TransportManager>.instance;
            NetManager net = Singleton<NetManager>.instance;

            ushort firstStop = tm.m_lines.m_buffer[lineId].m_stops;
            if (firstStop == 0)
            {
                return false;
            }

            // Use the node's own position: the stop may have been snapped somewhere
            // slightly different from the position we asked for.
            Vector3 closingPosition = net.m_nodes.m_buffer[firstStop].m_position;

            if (!tm.m_lines.m_buffer[lineId].AddStop(lineId, -1, closingPosition, firstSite.FixedPlatform))
            {
                return false;
            }

            return tm.m_lines.m_buffer[lineId].Complete;
        }

        /// <summary>
        /// Chooses a handful of spaced-out stop positions around the centre of the
        /// road network. Deliberately crude: the real corridor logic is a later
        /// module, and all this spike needs is a line that actually works.
        /// </summary>
        private static List<StopSite> PickStopSites(TransportInfo busInfo)
        {
            NetManager net = Singleton<NetManager>.instance;

            List<ushort> candidates = new List<ushort>();
            List<Vector3> candidateCentres = new List<Vector3>();
            Vector3 sum = Vector3.zero;

            for (int i = 1; i < net.m_segments.m_buffer.Length; i++)
            {
                if (!StopPlacement.IsSegmentUsableForStops((ushort)i, busInfo))
                {
                    continue;
                }

                Vector3 middle = net.m_segments.m_buffer[i].m_middlePosition;
                candidates.Add((ushort)i);
                candidateCentres.Add(middle);
                sum += middle;
            }

            Log.Info("Stop candidates (roads a bus can serve): " + candidates.Count);

            if (candidates.Count == 0)
            {
                return new List<StopSite>();
            }

            Vector3 centre = sum / candidates.Count;

            // Sort on the snapshot rather than re-reading segment positions: a comparer
            // whose inputs move underneath it makes the ordering inconsistent, which
            // List.Sort reports as an exception rather than a wrong answer.
            int[] order = new int[candidates.Count];
            float[] distance = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                order[i] = i;
                distance[i] = Vector3.SqrMagnitude(candidateCentres[i] - centre);
            }
            Array.Sort(distance, order);

            List<StopSite> picked = new List<StopSite>();
            float minSpacingSqr = MinStopSpacing * MinStopSpacing;
            int rejectedUnplaceable = 0;
            int rejectedNoAnchor = 0;

            for (int i = 0; i < order.Length && picked.Count < MaxStops; i++)
            {
                ushort segmentId = candidates[order[i]];

                Vector3 stopPos;
                bool fixedPlatform;
                if (!StopPlacement.TryGetStopPosition(segmentId, candidateCentres[order[i]], busInfo,
                                                      out stopPos, out fixedPlatform))
                {
                    rejectedUnplaceable++;
                    continue;
                }

                bool tooClose = false;
                for (int j = 0; j < picked.Count; j++)
                {
                    if (Vector3.SqrMagnitude(picked[j].Position - stopPos) < minSpacingSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                if (!StopPlacement.CanAnchorStop(stopPos, busInfo))
                {
                    rejectedNoAnchor++;
                    continue;
                }

                picked.Add(new StopSite(segmentId, stopPos, fixedPlatform));
            }

            Log.Info("Stop selection: " + picked.Count + " picked, " + rejectedUnplaceable +
                     " had no usable stop position, " + rejectedNoAnchor +
                     " could not be anchored by the path manager.");

            return picked;
        }

        /// <summary>
        /// Reports every line this spike created, with the details that matter for a
        /// round-trip comparison. Read-only, but it walks simulation structures, so it
        /// runs on the simulation thread like everything else here.
        /// </summary>
        public static void ReportTestLines()
        {
            if (!Singleton<TransportManager>.exists || !Singleton<SimulationManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to report.");
                return;
            }

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    WriteReport();
                }
                catch (Exception e)
                {
                    Log.Exception("ReportTestLines failed", e);
                }
            });
        }

        private static void WriteReport()
        {
            TransportManager tm = Singleton<TransportManager>.instance;
            NetManager net = Singleton<NetManager>.instance;
            StringBuilder sb = new StringBuilder("Test line report:");
            int found = 0;

            for (ushort lineId = 1; lineId < tm.m_lines.m_size; lineId++)
            {
                if ((tm.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                {
                    continue;
                }

                string name = tm.GetLineName(lineId);
                if (name == null || !name.StartsWith(TestLinePrefix))
                {
                    continue;
                }

                found++;

                int stopCount = tm.m_lines.m_buffer[lineId].CountStops(lineId);
                int notConnected = 0;
                int pathFailed = 0;
                int noLane = 0;

                for (int i = 0; i < stopCount; i++)
                {
                    ushort node = tm.m_lines.m_buffer[lineId].GetStop(i);
                    if (node == 0)
                    {
                        continue;
                    }

                    // The decisive one. A stop whose node never got a lane bound to it
                    // is dead: no Stop flag was written, no vehicle can serve it, and
                    // neither LineNotConnected nor PathFailed says so. Without this the
                    // report can call a broken line healthy.
                    if (!StopPlacement.HasLaneConnection(node))
                    {
                        noLane++;
                    }

                    if ((net.m_nodes.m_buffer[node].m_problems & Notification.Problem1.LineNotConnected).IsNotNone)
                    {
                        notConnected++;
                    }

                    for (int s = 0; s < 8; s++)
                    {
                        ushort seg = net.m_nodes.m_buffer[node].GetSegment(s);
                        if (seg != 0 &&
                            (net.m_segments.m_buffer[seg].m_flags & NetSegment.Flags.PathFailed) != NetSegment.Flags.None)
                        {
                            pathFailed++;
                            break;
                        }
                    }
                }

                sb.Append("\n  line ").Append(lineId)
                  .Append("  name='").Append(name).Append('\'')
                  .Append("\n    stops=").Append(stopCount)
                  .Append("  complete=").Append(tm.m_lines.m_buffer[lineId].Complete)
                  .Append("  flags=").Append(tm.m_lines.m_buffer[lineId].m_flags)
                  .Append("  budget=").Append(tm.m_lines.m_buffer[lineId].m_budget)
                  .Append("\n    totalLength=").Append(tm.m_lines.m_buffer[lineId].m_totalLength.ToString("F1"))
                  .Append("  targetVehicles=").Append(tm.m_lines.m_buffer[lineId].CalculateTargetVehicleCount())
                  .Append("  vehicles=").Append(CountVehicles(lineId))
                  .Append("\n    stopsNotConnected=").Append(notConnected)
                  .Append("  stopsOnFailedPaths=").Append(pathFailed)
                  .Append("  stopsWithNoLane=").Append(noLane);

                if (noLane != 0)
                {
                    sb.Append("\n    WARNING: ").Append(noLane)
                      .Append(" stop(s) have no lane bound. They are dead stops even though")
                      .Append(" no problem flag is set. If this was checked immediately after")
                      .Append(" building, retry once the simulation has run a few frames.");
                }
            }

            if (found == 0)
            {
                sb.Append("\n  (none found)");
            }

            Log.Info(sb.ToString());
        }

        private static int CountVehicles(ushort lineId)
        {
            VehicleManager vm = Singleton<VehicleManager>.instance;
            ushort vehicle = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_vehicles;
            int count = 0;

            while (vehicle != 0 && count < 16384)
            {
                count++;
                vehicle = vm.m_vehicles.m_buffer[vehicle].m_nextLineVehicle;
            }

            return count;
        }

        /// <summary>
        /// Removes every line this spike created. The prefix is checked on the
        /// simulation thread immediately before the release: line ids are recycled, so
        /// a list gathered earlier could by then name a line the player has just made.
        /// </summary>
        public static void RemoveTestLines()
        {
            if (!Singleton<TransportManager>.exists || !Singleton<SimulationManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to remove.");
                return;
            }

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    ReleaseTestLines();
                }
                catch (Exception e)
                {
                    Log.Exception("RemoveTestLines failed", e);
                }
            });
        }

        private static void ReleaseTestLines()
        {
            TransportManager tm = Singleton<TransportManager>.instance;
            int removed = 0;

            for (ushort lineId = 1; lineId < tm.m_lines.m_size; lineId++)
            {
                if ((tm.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                {
                    continue;
                }

                string name = tm.GetLineName(lineId);
                if (name == null || !name.StartsWith(TestLinePrefix))
                {
                    continue;
                }

                tm.ReleaseLine(lineId);
                removed++;
            }

            Log.Info(removed == 0 ? "No test lines to remove." : "Removed " + removed + " test line(s).");
        }

        private struct StopSite
        {
            public readonly ushort SegmentId;
            public readonly Vector3 Position;
            public readonly bool FixedPlatform;

            public StopSite(ushort segmentId, Vector3 position, bool fixedPlatform)
            {
                SegmentId = segmentId;
                Position = position;
                FixedPlatform = fixedPlatform;
            }
        }
    }
}
