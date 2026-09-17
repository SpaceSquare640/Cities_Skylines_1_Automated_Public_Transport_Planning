using System.Text;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// Picks the TransportInfo to plan city bus lines with.
    ///
    /// TransportType.Bus is not unique: a stock city has both "Bus" and
    /// "Intercity Bus", and the latter is meant for outside connections. Matching
    /// on the transport type alone can therefore pick the wrong prefab.
    ///
    /// Verified in-game: the two differ by ItemClass level — the city bus is Level1
    /// ("Bus Line"), the intercity one Level3 ("Intercity Bus Line") — so the level
    /// is the primary test, with the prefab name only as a fallback.
    /// </summary>
    public static class BusPrefabResolver
    {
        private const string IntercityMarker = "Intercity";

        /// <summary>
        /// Returns the city bus TransportInfo, or null if this save has none.
        /// </summary>
        public static TransportInfo ResolveCityBus()
        {
            TransportInfo exactMatch = null;
            TransportInfo firstNonIntercity = null;
            int busTypeCount = 0;

            int count = PrefabCollection<TransportInfo>.PrefabCount();
            for (uint i = 0; i < count; i++)
            {
                TransportInfo info = PrefabCollection<TransportInfo>.GetPrefab(i);
                if (info == null || info.m_transportType != TransportInfo.TransportType.Bus)
                {
                    continue;
                }

                busTypeCount++;

                // Verified in-game: the city bus prefab is ItemClass.Level.Level1
                // ("Bus Line"), the intercity one is Level3 ("Intercity Bus Line").
                // The level is a stable enum, so prefer it over the prefab name.
                if (info.m_class != null && info.m_class.m_level == ItemClass.Level.Level1)
                {
                    exactMatch = info;
                }
                else if (firstNonIntercity == null &&
                         info.name != null && info.name.IndexOf(IntercityMarker) < 0)
                {
                    firstNonIntercity = info;
                }
            }

            TransportInfo chosen = exactMatch ?? firstNonIntercity;

            if (chosen == null)
            {
                Log.Warning("No city bus TransportInfo found (" + busTypeCount + " prefabs of type Bus were examined).");
            }
            else
            {
                Log.Info("City bus prefab resolved to '" + chosen.name + "' out of " + busTypeCount + " Bus-type prefabs.");
            }

            return chosen;
        }

        /// <summary>
        /// Dumps the fields that might let us tell city buses from intercity buses
        /// on something better than a name. Diagnostic only.
        /// </summary>
        public static void DumpBusPrefabs()
        {
            StringBuilder sb = new StringBuilder("Bus-type prefabs in detail:");

            int count = PrefabCollection<TransportInfo>.PrefabCount();
            for (uint i = 0; i < count; i++)
            {
                TransportInfo info = PrefabCollection<TransportInfo>.GetPrefab(i);
                if (info == null || info.m_transportType != TransportInfo.TransportType.Bus)
                {
                    continue;
                }

                sb.Append("\n  name=").Append(info.name)
                  .Append("\n    class=").Append(info.m_class != null ? info.m_class.name : "null")
                  .Append("  service=").Append(info.m_class != null ? info.m_class.m_service.ToString() : "?")
                  .Append("  subService=").Append(info.m_class != null ? info.m_class.m_subService.ToString() : "?")
                  .Append("  level=").Append(info.m_class != null ? info.m_class.m_level.ToString() : "?")
                  .Append("\n    netService=").Append(info.m_netService)
                  .Append("  stationService=").Append(info.m_stationService)
                  .Append("  vehicleType=").Append(info.m_vehicleType)
                  .Append("\n    defaultVehicleDistance=").Append(info.m_defaultVehicleDistance)
                  .Append("  stopFlag=").Append(info.m_stopFlag)
                  .Append("  netInfo=").Append(info.m_netInfo != null ? info.m_netInfo.name : "null");
            }

            Log.Info(sb.ToString());
        }
    }
}
