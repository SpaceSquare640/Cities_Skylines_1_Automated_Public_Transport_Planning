using ICities;
using AutomatedPublicTransportPlanning.Planning;
using AutomatedPublicTransportPlanning.Spike;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning
{
    /// <summary>
    /// Entry point the game looks for when it scans an assembly in the mods folder.
    /// Instantiated while the main menu loads, long before any city exists, so it
    /// must not touch simulation state.
    /// </summary>
    public sealed class Mod : IUserMod
    {
        public const string ModName = "Automated Public Transport Planning";
        public const string Version = "0.1.0";

        public string Name
        {
            get { return ModName + " " + Version; }
        }

        public string Description
        {
            get { return "Plans bus lines from city demand. Previews first; nothing is built until you accept a line."; }
        }

        /// <summary>
        /// Called when the player opens this mod's entry in the content manager.
        /// UIHelperBase only covers simple option controls; the in-game planning
        /// panel is built separately against the game's own UI framework.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            Log.Info("Settings UI requested.");

            UIHelperBase diagnostics = helper.AddGroup("Diagnostics");
            diagnostics.AddButton("Write a test line to the log", Guarded("diagnostics", OnDiagnosticsButton));
            diagnostics.AddButton("List bus prefabs in detail", Guarded("bus prefab dump", BusPrefabResolver.DumpBusPrefabs));

            // Throwaway controls for the save round-trip spike. These write to the
            // loaded city, so they are kept in their own clearly labelled group and
            // will be removed once the spike has served its purpose.
            UIHelperBase spike = helper.AddGroup("Spike 1 - save round trip (writes to your city)");
            spike.AddButton("1. Create a test bus line", Guarded("create test line", SaveRoundTripSpike.CreateTestLine));
            spike.AddButton("2. Report test lines", Guarded("report test lines", SaveRoundTripSpike.ReportTestLines));
            spike.AddButton("3. Remove test lines", Guarded("remove test lines", SaveRoundTripSpike.RemoveTestLines));
        }

        /// <summary>
        /// Wraps a button handler so nothing it throws reaches the game's UI event
        /// dispatcher.
        /// </summary>
        private static OnButtonClicked Guarded(string context, OnButtonClicked handler)
        {
            return delegate { Log.Guard(context, delegate { handler(); }); };
        }

        private static void OnDiagnosticsButton()
        {
            Log.Info("Diagnostics button pressed. Mod assembly is loaded and responding.");
        }
    }
}
