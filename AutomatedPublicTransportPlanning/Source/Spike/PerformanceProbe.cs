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
    /// A5-1: measures the operations the M3 performance budget is built on.
    ///
    /// The budget in the M3 design is an estimate. Two of its lines were guessed
    /// badly enough in the first draft to change the architecture — TryGetStopPosition
    /// was assumed cheap enough to run over every segment, and CanAnchorStop was left
    /// out of the budget entirely. This measures both rather than arguing about them.
    ///
    /// Throwaway diagnostic code. It reads simulation state and writes nothing.
    ///
    /// Two things make a naive version of this measure the wrong thing:
    ///
    ///   Stopwatch overhead. Starting and stopping a timer around a call that costs
    ///     a microsecond measures the timer. Everything here times a batch and
    ///     divides, so the fixed cost is amortised across thousands of calls.
    ///
    ///   JIT compilation. The first call to a method includes the cost of compiling
    ///     it. Every phase runs a warm-up pass that is not timed.
    ///
    /// All of it runs on the simulation thread, and AddAction does not split work
    /// across frames — the whole delegate runs inside one simulation step. Each phase
    /// is therefore bounded by both a sample count and a time budget, so the worst
    /// case is a brief hitch rather than a freeze.
    /// </summary>
    internal static class PerformanceProbe
    {
        /// <summary>Per-phase ceiling. A phase stops sampling when it runs out of either.</summary>
        private const int StopPositionSamples = 3000;
        private const int AnchorSamples = 600;
        private const int WarmupSamples = 64;

        private const double PhaseBudgetMs = 250.0;

        /// <summary>Guards against a corrupt citizen unit chain looping forever.</summary>
        private const int MaxUnitsPerBuilding = 512;

        public static void Run()
        {
            if (!Singleton<NetManager>.exists || !Singleton<BuildingManager>.exists ||
                !Singleton<SimulationManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to measure.");
                return;
            }

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    Measure();
                }
                catch (Exception e)
                {
                    Log.Exception("PerformanceProbe failed", e);
                }
            });
        }

        private static void Measure()
        {
            TransportInfo busInfo = BusPrefabResolver.ResolveCityBus();
            if (busInfo == null)
            {
                Log.Error("Cannot measure: no city bus prefab in this save.");
                return;
            }

            StringBuilder sb = new StringBuilder("A5-1 performance probe");
            sb.Append("\n  timer resolution: ")
              .Append((1000000.0 / (double)System.Diagnostics.Stopwatch.Frequency).ToString("F3"))
              .Append(" us/tick, highRes=")
              .Append(System.Diagnostics.Stopwatch.IsHighResolution);

            List<ushort> usable = new List<ushort>();

            MeasureSegmentScan(busInfo, sb, usable);
            MeasureStopPosition(busInfo, sb, usable);
            MeasureCitizenUnits(sb);
            ReportVehicleCensus(sb);

            Log.Info(sb.ToString());
        }

        // ---------------------------------------------------------------- phase 1

        /// <summary>
        /// The segment walk the StopGraph has to do. Measured twice: once reading only
        /// what is free, once with the eligibility filter, so the filter's own cost is
        /// the difference rather than a guess.
        /// </summary>
        private static void MeasureSegmentScan(TransportInfo busInfo, StringBuilder sb, List<ushort> usable)
        {
            NetManager net = Singleton<NetManager>.instance;
            int length = net.m_segments.m_buffer.Length;

            // Warm up both paths before either is timed.
            for (int i = 1; i < length && i < WarmupSamples; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None)
                {
                    float ignored = net.m_segments.m_buffer[i].m_averageLength;
                    StopPlacement.IsSegmentUsableForStops((ushort)i, busInfo);
                }
            }

            // Pass A: flags plus m_averageLength. This is the claim that reading the
            // length the game already computed is free; if it is not, the whole edge
            // cost model needs rethinking.
            int created = 0;
            float lengthSum = 0f;
            System.Diagnostics.Stopwatch swA = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 1; i < length; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    continue;
                }

                created++;
                lengthSum += net.m_segments.m_buffer[i].m_averageLength;
            }
            swA.Stop();

            // Pass B: the same walk plus the eligibility test.
            int eligible = 0;
            System.Diagnostics.Stopwatch swB = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 1; i < length; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    continue;
                }

                if (StopPlacement.IsSegmentUsableForStops((ushort)i, busInfo))
                {
                    eligible++;
                }
            }
            swB.Stop();

            // Collected outside the timed passes so the list growth is not measured.
            for (int i = 1; i < length; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None &&
                    StopPlacement.IsSegmentUsableForStops((ushort)i, busInfo))
                {
                    usable.Add((ushort)i);
                }
            }

            sb.Append("\n\n  [1] segment walk   buffer=").Append(length)
              .Append("  created=").Append(created)
              .Append("  busEligible=").Append(eligible)
              .Append("\n      flags + m_averageLength   ").Append(Millis(swA).ToString("F2")).Append(" ms")
              .Append("   (").Append(MicrosPer(swA, created).ToString("F3")).Append(" us/segment)")
              .Append("\n      + IsSegmentUsableForStops ").Append(Millis(swB).ToString("F2")).Append(" ms")
              .Append("   (").Append(MicrosPer(swB, created).ToString("F3")).Append(" us/segment)")
              .Append("\n      filter alone              ")
              .Append((Millis(swB) - Millis(swA)).ToString("F2")).Append(" ms")
              .Append("\n      (lengthSum=").Append(lengthSum.ToString("F0")).Append(" m, kept so the read is not optimised away)");
        }

        // ---------------------------------------------------------------- phase 2 and 3

        /// <summary>
        /// The two calls the first design budgeted wrongly. Samples are taken with a
        /// stride across the eligible list rather than from the front, so the result is
        /// not dominated by whichever corner of the map happens to be indexed first.
        /// </summary>
        private static void MeasureStopPosition(TransportInfo busInfo, StringBuilder sb, List<ushort> usable)
        {
            if (usable.Count == 0)
            {
                sb.Append("\n\n  [2] no bus-eligible segments; stop position and anchor phases skipped.");
                return;
            }

            NetManager net = Singleton<NetManager>.instance;
            int stride = Mathf.Max(1, usable.Count / StopPositionSamples);

            Vector3 pos;
            bool platform;

            for (int i = 0; i < usable.Count && i < WarmupSamples; i++)
            {
                StopPlacement.TryGetStopPosition(usable[i], net.m_segments.m_buffer[usable[i]].m_middlePosition,
                                                 busInfo, out pos, out platform);
            }

            List<Vector3> found = new List<Vector3>();
            int attempted = 0;
            int succeeded = 0;

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < usable.Count && attempted < StopPositionSamples; i += stride)
            {
                ushort segmentId = usable[i];
                attempted++;

                if (StopPlacement.TryGetStopPosition(segmentId, net.m_segments.m_buffer[segmentId].m_middlePosition,
                                                    busInfo, out pos, out platform))
                {
                    succeeded++;
                    found.Add(pos);
                }

                if ((attempted & 255) == 0 && Millis(sw) > PhaseBudgetMs)
                {
                    break;
                }
            }
            sw.Stop();

            sb.Append("\n\n  [2] TryGetStopPosition   samples=").Append(attempted)
              .Append("  succeeded=").Append(succeeded)
              .Append("\n      ").Append(Millis(sw).ToString("F2")).Append(" ms total")
              .Append("   (").Append(MicrosPer(sw, attempted).ToString("F2")).Append(" us/call)")
              .Append("\n      projected over ").Append(usable.Count).Append(" eligible segments: ")
              .Append((MicrosPer(sw, attempted) * usable.Count / 1000.0).ToString("F1")).Append(" ms")
              .Append("\n      ^ this is the number that decides whether stop positions can be")
              .Append("\n        precomputed when the graph is built, or must stay lazy.");

            MeasureAnchor(busInfo, sb, found);
        }

        private static void MeasureAnchor(TransportInfo busInfo, StringBuilder sb, List<Vector3> positions)
        {
            if (positions.Count == 0)
            {
                sb.Append("\n\n  [3] no stop positions produced; anchor phase skipped.");
                return;
            }

            for (int i = 0; i < positions.Count && i < WarmupSamples; i++)
            {
                StopPlacement.CanAnchorStop(positions[i], busInfo);
            }

            int stride = Mathf.Max(1, positions.Count / AnchorSamples);
            int attempted = 0;
            int anchored = 0;

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < positions.Count && attempted < AnchorSamples; i += stride)
            {
                attempted++;
                if (StopPlacement.CanAnchorStop(positions[i], busInfo))
                {
                    anchored++;
                }

                if ((attempted & 63) == 0 && Millis(sw) > PhaseBudgetMs)
                {
                    break;
                }
            }
            sw.Stop();

            sb.Append("\n\n  [3] CanAnchorStop   samples=").Append(attempted)
              .Append("  anchored=").Append(anchored)
              .Append("\n      ").Append(Millis(sw).ToString("F2")).Append(" ms total")
              .Append("   (").Append(MicrosPer(sw, attempted).ToString("F2")).Append(" us/call)")
              .Append("\n      ^ absent from the first budget entirely. It calls FindPathPosition,")
              .Append("\n        which sweeps a 4x4 block of the segment grid.");
        }

        // ---------------------------------------------------------------- phase 4

        /// <summary>
        /// R14: the DemandField fill reads capacity from the citizen unit chain rather
        /// than from Building.m_citizenCount, which is a byte and saturates. Walking a
        /// linked list per building is the price of that, and it has never been measured.
        /// </summary>
        private static void MeasureCitizenUnits(StringBuilder sb)
        {
            if (!Singleton<CitizenManager>.exists)
            {
                sb.Append("\n\n  [4] no CitizenManager; demand field phase skipped.");
                return;
            }

            BuildingManager buildings = Singleton<BuildingManager>.instance;
            CitizenManager citizens = Singleton<CitizenManager>.instance;
            int length = buildings.m_buildings.m_buffer.Length;

            for (int i = 1; i < length && i < WarmupSamples; i++)
            {
                if ((buildings.m_buildings.m_buffer[i].m_flags & Building.Flags.Created) != Building.Flags.None)
                {
                    WalkUnits(citizens, buildings.m_buildings.m_buffer[i].m_citizenUnits);
                }
            }

            // Pass A: the building walk on its own.
            int created = 0;
            System.Diagnostics.Stopwatch swA = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 1; i < length; i++)
            {
                if ((buildings.m_buildings.m_buffer[i].m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                created++;
            }
            swA.Stop();

            // Pass B: the same walk plus the citizen unit chains.
            int homes = 0;
            int works = 0;
            int visits = 0;
            int units = 0;
            System.Diagnostics.Stopwatch swB = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 1; i < length; i++)
            {
                if ((buildings.m_buildings.m_buffer[i].m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                uint unit = buildings.m_buildings.m_buffer[i].m_citizenUnits;
                int guard = 0;
                while (unit != 0U && guard < MaxUnitsPerBuilding)
                {
                    CitizenUnit.Flags flags = citizens.m_units.m_buffer[unit].m_flags;

                    if ((ushort)(flags & CitizenUnit.Flags.Home) != 0) { homes++; }
                    if ((ushort)(flags & CitizenUnit.Flags.Work) != 0) { works++; }
                    if ((ushort)(flags & CitizenUnit.Flags.Visit) != 0) { visits++; }

                    units++;
                    guard++;
                    unit = citizens.m_units.m_buffer[unit].m_nextUnit;
                }
            }
            swB.Stop();

            sb.Append("\n\n  [4] demand field fill   buildings=").Append(created)
              .Append("  units=").Append(units)
              .Append("\n      building walk only   ").Append(Millis(swA).ToString("F2")).Append(" ms")
              .Append("\n      + citizen unit chain ").Append(Millis(swB).ToString("F2")).Append(" ms")
              .Append("   (").Append(MicrosPer(swB, created).ToString("F2")).Append(" us/building)")
              .Append("\n      chain alone          ")
              .Append((Millis(swB) - Millis(swA)).ToString("F2")).Append(" ms")
              .Append("\n      capacity: homes=").Append(homes)
              .Append("  workUnits=").Append(works).Append(" (x5 = ").Append(works * 5).Append(" jobs)")
              .Append("  visitUnits=").Append(visits).Append(" (x5 = ").Append(visits * 5).Append(" slots)");
        }

        private static void WalkUnits(CitizenManager citizens, uint firstUnit)
        {
            uint unit = firstUnit;
            int guard = 0;
            while (unit != 0U && guard < MaxUnitsPerBuilding)
            {
                guard++;
                unit = citizens.m_units.m_buffer[unit].m_nextUnit;
            }
        }

        // ---------------------------------------------------------------- phase 5

        /// <summary>
        /// R15: a line reported targetVehicles=12 while running one. Listing every line
        /// in the city says whether that is something about the lines this mod makes or
        /// a condition the whole city is in — the player's own hand-drawn lines are the
        /// control group.
        /// </summary>
        private static void ReportVehicleCensus(StringBuilder sb)
        {
            if (!Singleton<TransportManager>.exists)
            {
                sb.Append("\n\n  [5] no TransportManager; vehicle census skipped.");
                return;
            }

            TransportManager tm = Singleton<TransportManager>.instance;
            sb.Append("\n\n  [5] vehicle census (R15)");

            int lines = 0;
            int starved = 0;
            int targetSum = 0;
            int actualSum = 0;

            for (ushort lineId = 1; lineId < tm.m_lines.m_size; lineId++)
            {
                if ((tm.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                {
                    continue;
                }

                lines++;

                int target = tm.m_lines.m_buffer[lineId].CalculateTargetVehicleCount();
                int actual = tm.m_lines.m_buffer[lineId].CountVehicles(lineId);
                targetSum += target;
                actualSum += actual;

                if (actual < target)
                {
                    starved++;
                }

                string name = tm.GetLineName(lineId);
                bool ours = name != null && name.StartsWith(SaveRoundTripSpike.TestLinePrefix);

                // Only the outliers and our own lines are listed; a city can have
                // hundreds of lines and the totals carry the answer.
                if (ours || actual * 2 < target)
                {
                    sb.Append("\n      line ").Append(lineId)
                      .Append(ours ? " [ours] " : "        ")
                      .Append("target=").Append(target)
                      .Append("  actual=").Append(actual)
                      .Append("  budget=").Append(tm.m_lines.m_buffer[lineId].m_budget)
                      .Append("  length=").Append(tm.m_lines.m_buffer[lineId].m_totalLength.ToString("F0"))
                      .Append("  '").Append(name).Append('\'');
                }
            }

            sb.Append("\n      lines=").Append(lines)
              .Append("  belowTarget=").Append(starved)
              .Append("  targetTotal=").Append(targetSum)
              .Append("  actualTotal=").Append(actualSum);

            if (lines > 0 && starved * 2 > lines)
            {
                sb.Append("\n      NOTE: most of the city's lines are below target, including ones")
                  .Append("\n            this mod did not create. That points at depots or budget,")
                  .Append("\n            not at how the line was built.");
            }

            // The target vehicle count scales with the city budget slider, so it is
            // part of the answer. Day and night are separate sliders in this game.
            if (Singleton<EconomyManager>.exists)
            {
                EconomyManager economy = Singleton<EconomyManager>.instance;
                sb.Append("\n      busBudget day=")
                  .Append(economy.GetBudget(ItemClass.Service.PublicTransport,
                                            ItemClass.SubService.PublicTransportBus, false))
                  .Append("  night=")
                  .Append(economy.GetBudget(ItemClass.Service.PublicTransport,
                                            ItemClass.SubService.PublicTransportBus, true));
            }
        }

        // ---------------------------------------------------------------- helpers

        private static double Millis(System.Diagnostics.Stopwatch sw)
        {
            return (double)sw.ElapsedTicks * 1000.0 / (double)System.Diagnostics.Stopwatch.Frequency;
        }

        private static double MicrosPer(System.Diagnostics.Stopwatch sw, int count)
        {
            if (count <= 0)
            {
                return 0.0;
            }

            return (double)sw.ElapsedTicks * 1000000.0 / (double)System.Diagnostics.Stopwatch.Frequency / (double)count;
        }
    }
}
