using System;
using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
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
    /// </summary>
    public static class SaveRoundTripSpike
    {
        /// <summary>Prefix that marks a line as belonging to this spike.</summary>
        public const string TestLinePrefix = "APTP Test";

        private const int MaxStops = 6;
        private const int MinStops = 3;
        private const float MinStopSpacing = 150f;

        /// <summary>
        /// Builds one test bus line near the middle of the built-up area.
        /// </summary>
        public static void CreateTestLine()
        {
            if (!Singleton<TransportManager>.exists || !Singleton<NetManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to do.");
                return;
            }

            try
            {
                TransportInfo busInfo = BusPrefabResolver.ResolveCityBus();
                if (busInfo == null)
                {
                    Log.Error("Cannot create a test line: no city bus prefab in this save.");
                    return;
                }

                List<StopSite> sites = PickStopSites(busInfo);
                if (sites.Count < MinStops)
                {
                    Log.Error("Only found " + sites.Count + " usable stop positions; need at least " + MinStops + ".");
                    return;
                }

                Log.Info("Picked " + sites.Count + " stop positions. Handing the build over to the simulation thread.");

                // Simulation state must not be written from the UI thread.
                Singleton<SimulationManager>.instance.AddAction(delegate
                {
                    BuildLine(busInfo, sites);
                });
            }
            catch (Exception e)
            {
                Log.Exception("CreateTestLine failed", e);
            }
        }

        private static void BuildLine(TransportInfo busInfo, List<StopSite> sites)
        {
            try
            {
                TransportManager tm = Singleton<TransportManager>.instance;

                ushort lineId;
                if (!tm.CreateLine(out lineId, ref Singleton<SimulationManager>.instance.m_randomizer, busInfo, true))
                {
                    Log.Error("CreateLine returned false. The line limit may have been reached.");
                    return;
                }

                Log.Info("Created line " + lineId + ". Adding stops.");

                int added = 0;
                for (int i = 0; i < sites.Count; i++)
                {
                    // index -1 appends to the end of the line.
                    bool ok = tm.m_lines.m_buffer[lineId].AddStop(lineId, -1, sites[i].Position, sites[i].FixedPlatform);
                    if (ok)
                    {
                        added++;
                    }
                    else
                    {
                        Log.Warning("AddStop rejected stop " + i + " at " + sites[i].Position + " (segment " + sites[i].SegmentId + ").");
                    }
                }

                string name = TestLinePrefix + " " + DateTime.Now.ToString("HH:mm:ss");
                IEnumerator<bool> rename = tm.SetLineName(lineId, name);
                while (rename.MoveNext())
                {
                    // SetLineName does its work before it yields, so draining it here is enough.
                }

                tm.UpdateLine(lineId);

                Log.Info("Line " + lineId + " named '" + name + "' with " + added + " of " + sites.Count +
                         " stops added. Save, reload, then use the report button to compare.");
            }
            catch (Exception e)
            {
                Log.Exception("BuildLine failed", e);
            }
        }

        /// <summary>
        /// Chooses a handful of spaced-out stop positions around the centre of the
        /// road network. This is deliberately crude: the real corridor logic is a
        /// later module, and all this spike needs is a line that actually works.
        /// </summary>
        private static List<StopSite> PickStopSites(TransportInfo busInfo)
        {
            NetManager net = Singleton<NetManager>.instance;
            List<ushort> candidates = new List<ushort>();
            Vector3 sum = Vector3.zero;

            for (int i = 1; i < net.m_segments.m_buffer.Length; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    continue;
                }

                if (!StopPlacement.IsSegmentUsableForStops((ushort)i, busInfo))
                {
                    continue;
                }

                candidates.Add((ushort)i);
                sum += net.m_segments.m_buffer[i].m_middlePosition;
            }

            Log.Info("Stop candidates (roads a bus can serve): " + candidates.Count);

            if (candidates.Count == 0)
            {
                return new List<StopSite>();
            }

            Vector3 centre = sum / candidates.Count;

            // Nearest to the centre first, so the test line lands in the built-up area.
            candidates.Sort(delegate (ushort a, ushort b)
            {
                float da = Vector3.SqrMagnitude(net.m_segments.m_buffer[a].m_middlePosition - centre);
                float db = Vector3.SqrMagnitude(net.m_segments.m_buffer[b].m_middlePosition - centre);
                return da.CompareTo(db);
            });

            List<StopSite> picked = new List<StopSite>();
            float minSpacingSqr = MinStopSpacing * MinStopSpacing;
            int rejectedUnplaceable = 0;
            int rejectedUnreachable = 0;

            for (int i = 0; i < candidates.Count && picked.Count < MaxStops; i++)
            {
                ushort segmentId = candidates[i];
                Vector3 near = net.m_segments.m_buffer[segmentId].m_middlePosition;

                Vector3 stopPos;
                bool fixedPlatform;
                if (!StopPlacement.TryGetStopPosition(segmentId, near, busInfo, out stopPos, out fixedPlatform))
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

                if (!StopPlacement.IsReachable(stopPos, busInfo))
                {
                    rejectedUnreachable++;
                    continue;
                }

                picked.Add(new StopSite(segmentId, stopPos, fixedPlatform));
            }

            Log.Info("Stop selection: " + picked.Count + " picked, " + rejectedUnplaceable +
                     " had no usable pedestrian lane, " + rejectedUnreachable + " failed the reachability check.");

            return picked;
        }

        /// <summary>
        /// Reports every line this spike created, with the details that matter for a
        /// round-trip comparison. Read-only.
        /// </summary>
        public static void ReportTestLines()
        {
            if (!Singleton<TransportManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to report.");
                return;
            }

            try
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
                    TransportLine.Flags flags = tm.m_lines.m_buffer[lineId].m_flags;

                    int pathFailed = 0;
                    int notConnected = 0;
                    for (int i = 0; i < stopCount; i++)
                    {
                        ushort node = tm.m_lines.m_buffer[lineId].GetStop(i);
                        if (node == 0)
                        {
                            continue;
                        }

                        // Only LineNotConnected matters here; other node problems are
                        // unrelated to whether the line works.
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
                      .Append("  flags=").Append(flags)
                      .Append("  budget=").Append(tm.m_lines.m_buffer[lineId].m_budget)
                      .Append("\n    totalLength=").Append(tm.m_lines.m_buffer[lineId].m_totalLength.ToString("F1"))
                      .Append("  targetVehicles=").Append(tm.m_lines.m_buffer[lineId].CalculateTargetVehicleCount())
                      .Append("\n    stopsNotConnected=").Append(notConnected)
                      .Append("  stopsOnFailedPaths=").Append(pathFailed);
                }

                if (found == 0)
                {
                    sb.Append("\n  (none found)");
                }

                Log.Info(sb.ToString());
            }
            catch (Exception e)
            {
                Log.Exception("ReportTestLines failed", e);
            }
        }

        /// <summary>
        /// Removes every line this spike created. Only lines carrying the test prefix
        /// are touched; anything the player made is left alone.
        /// </summary>
        public static void RemoveTestLines()
        {
            if (!Singleton<TransportManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to remove.");
                return;
            }

            List<ushort> doomed = new List<ushort>();
            TransportManager tm = Singleton<TransportManager>.instance;

            for (ushort lineId = 1; lineId < tm.m_lines.m_size; lineId++)
            {
                if ((tm.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                {
                    continue;
                }

                string name = tm.GetLineName(lineId);
                if (name != null && name.StartsWith(TestLinePrefix))
                {
                    doomed.Add(lineId);
                }
            }

            if (doomed.Count == 0)
            {
                Log.Info("No test lines to remove.");
                return;
            }

            Log.Info("Removing " + doomed.Count + " test line(s): " + string.Join(", ", doomed.ConvertAll(delegate (ushort id) { return id.ToString(); }).ToArray()));

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    for (int i = 0; i < doomed.Count; i++)
                    {
                        Singleton<TransportManager>.instance.ReleaseLine(doomed[i]);
                    }
                    Log.Info("Test lines removed.");
                }
                catch (Exception e)
                {
                    Log.Exception("RemoveTestLines failed", e);
                }
            });
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
