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

            // Read-only, so it is not behind the spike arming checkbox. It does cost a
            // brief hitch: every phase is bounded, but they all run in one simulation
            // step because AddAction cannot split work across frames.
            diagnostics.AddButton("Measure planning costs (brief pause)",
                                  Guarded("performance probe", PerformanceProbe.Run));

            // Throwaway controls for the save round-trip spike. These write to the
            // loaded city, so they are kept in their own clearly labelled group and
            // will be removed once the spike has served its purpose.
            //
            // Buttons 1 and 3 change the player's city, and this settings page is
            // reachable by anyone who merely opens the content manager. They are
            // therefore disarmed until the checkbox below is ticked, so that no single
            // stray click can alter a save. The arming resets every time the page is
            // opened; it is deliberately not remembered.
            s_spikeArmed = false;

            UIHelperBase spike = helper.AddGroup("Spike 1 - save round trip (writes to your city)");
            spike.AddCheckbox("Enable these buttons - they change the city you have loaded",
                              false, OnSpikeArmedChanged);
            spike.AddButton("1. Create a test bus line", Guarded("create test line", ArmedOnly(SaveRoundTripSpike.CreateTestLine)));
            spike.AddButton("2. Report test lines", Guarded("report test lines", SaveRoundTripSpike.ReportTestLines));
            spike.AddButton("3. Remove test lines", Guarded("remove test lines", ArmedOnly(SaveRoundTripSpike.RemoveTestLines)));
        }

        /// <summary>
        /// Whether the spike's writing buttons are currently allowed to act. Static
        /// because OnSettingsUI hands the game delegates that outlive this call.
        /// </summary>
        private static bool s_spikeArmed;

        private static void OnSpikeArmedChanged(bool isChecked)
        {
            s_spikeArmed = isChecked;
            Log.Info(isChecked
                ? "Spike buttons armed. Buttons 1 and 3 will now change the loaded city."
                : "Spike buttons disarmed.");
        }

        /// <summary>
        /// Blocks a handler until the player has explicitly armed the spike group.
        /// Button 2 is read-only and is not wrapped.
        /// </summary>
        private static OnButtonClicked ArmedOnly(OnButtonClicked handler)
        {
            return delegate
            {
                if (!s_spikeArmed)
                {
                    Log.Warning("Ignored: tick the checkbox above first. These buttons change your city.");
                    return;
                }

                handler();
            };
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
