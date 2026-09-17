using System;
using System.Text;
using ColossalFramework;
using ICities;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning
{
    /// <summary>
    /// Runs once a city is loaded. For now it only reports what the planner would
    /// have to work with, so the assumptions behind M1 (city survey) can be checked
    /// against a real save before any planning code is written.
    ///
    /// Everything here is read-only. Nothing is created, modified or deleted.
    /// </summary>
    public sealed class Loader : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame &&
                mode != LoadMode.NewGameFromScenario)
            {
                Log.Info("Level loaded in mode " + mode + "; survey skipped.");
                return;
            }

            try
            {
                Log.Info("City loaded (" + mode + "). Running read-only survey.");
                ReportTransportPrefabs();
                ReportBusDepots();
                ReportStopCapableSegments();
                Log.Info("Survey complete.");
            }
            catch (Exception e)
            {
                Log.Exception("Survey failed", e);
            }
        }

        /// <summary>
        /// Which transport modes this player actually has. Enumerating the prefab
        /// collection avoids hard-coding any DLC mapping: a mode is available if and
        /// only if a prefab for it was loaded.
        /// </summary>
        private static void ReportTransportPrefabs()
        {
            int count = PrefabCollection<TransportInfo>.PrefabCount();
            StringBuilder sb = new StringBuilder();
            sb.Append("Transport prefabs available: ").Append(count);

            int busPrefabs = 0;
            for (uint i = 0; i < count; i++)
            {
                TransportInfo info = PrefabCollection<TransportInfo>.GetPrefab(i);
                if (info == null)
                {
                    continue;
                }

                sb.Append("\n  - ").Append(info.name).Append("  [").Append(info.m_transportType).Append(']');

                if (info.m_transportType == TransportInfo.TransportType.Bus)
                {
                    busPrefabs++;
                }
            }

            Log.Info(sb.ToString());

            if (busPrefabs == 0)
            {
                Log.Warning("No bus TransportInfo found. Bus planning would not be possible in this save.");
            }
        }

        /// <summary>
        /// Vehicles are supplied by depots through the TransferManager, so a line
        /// without a depot for its mode will never run anything. This is a coarse
        /// "does one exist at all" check; whether a given depot can actually serve a
        /// given line is a separate question the planner cannot answer yet.
        /// </summary>
        private static void ReportBusDepots()
        {
            BuildingManager buildings = Singleton<BuildingManager>.instance;

            // Matching on TransportType.Bus alone also counts intercity bus depots,
            // which cannot serve a city line. Compare against the resolved city bus
            // prefab instead.
            TransportInfo cityBus = Planning.BusPrefabResolver.ResolveCityBus();
            if (cityBus == null)
            {
                Log.Warning("No city bus prefab; skipping the depot count.");
                return;
            }

            int total = 0;
            int busDepots = 0;

            for (int i = 1; i < buildings.m_buildings.m_buffer.Length; i++)
            {
                if ((buildings.m_buildings.m_buffer[i].m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                total++;

                BuildingInfo info = buildings.m_buildings.m_buffer[i].Info;
                if (info == null)
                {
                    continue;
                }

                DepotAI depot = info.m_buildingAI as DepotAI;
                if (depot != null && depot.m_transportInfo == cityBus)
                {
                    busDepots++;
                }
            }

            Log.Info("Buildings: " + total + ", of which city bus depots: " + busDepots);

            if (busDepots == 0)
            {
                Log.Warning("No bus depot in this city. Any planned bus line would have no vehicles.");
            }
        }

        /// <summary>
        /// A bus stop attaches to a pedestrian lane on a road segment, so the size of
        /// the stop candidate pool is bounded by how many segments carry one.
        /// </summary>
        private static void ReportStopCapableSegments()
        {
            NetManager net = Singleton<NetManager>.instance;
            int created = 0;
            int withPedestrianLane = 0;

            for (int i = 1; i < net.m_segments.m_buffer.Length; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    continue;
                }

                created++;

                NetInfo info = net.m_segments.m_buffer[i].Info;
                if (info == null)
                {
                    continue;
                }

                if ((info.m_laneTypes & NetInfo.LaneType.Pedestrian) != NetInfo.LaneType.None)
                {
                    withPedestrianLane++;
                }
            }

            Log.Info("Net segments: " + created + ", with a pedestrian lane: " + withPedestrianLane);
        }
    }
}
