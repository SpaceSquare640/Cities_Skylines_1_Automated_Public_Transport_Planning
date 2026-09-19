using System.Collections.Generic;
using ColossalFramework.UI;
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

        /// <summary>
        /// The name stays in English in every language. It is how the mod is found on
        /// the workshop and how players refer to it to each other; a translated product
        /// name makes it unsearchable for the very players it was translated for.
        /// </summary>
        public string Name
        {
            get { return ModName + " " + Version; }
        }

        public string Description
        {
            get { return Loc.Get(Strings.ModDescription); }
        }

        /// <summary>
        /// Every component whose text follows the language, paired with the key that
        /// supplies it.
        ///
        /// The list is cleared at the top of OnSettingsUI, which is the one moment the
        /// game guarantees the previous panel is gone. Holding these references is
        /// otherwise safe: they are components this mod created, handed back by the
        /// game's own API, and setting text on one is exactly what UIHelper does when
        /// it builds them.
        /// </summary>
        private static readonly List<KeyValuePair<object, string>> s_localised =
            new List<KeyValuePair<object, string>>();

        /// <summary>
        /// Called when the player opens this mod's entry in the content manager.
        /// UIHelperBase only covers simple option controls; the in-game planning
        /// panel is built separately against the game's own UI framework.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // The game builds this panel from scratch each time and discards the last
            // one, so anything tracked from a previous visit is already dead.
            s_localised.Clear();

            // Logged in English on purpose, and it names the language in use. When a
            // player reports that the panel is in the wrong language, this line says
            // whether the setting was read correctly or the table is what is wrong.
            Log.Info("Settings UI requested. Language is '" + Loc.CurrentLanguage + "'.");

            // Deliberately not inside a group: a group would add a title that also needs
            // retranslating, and the dropdown's own label already says what it is.
            UIDropDown picker = helper.AddDropdown(Loc.Get(Strings.LabelLanguage),
                                                   Loc.LanguageNames,
                                                   Loc.CurrentIndex,
                                                   OnLanguageChanged) as UIDropDown;
            TrackDropdownLabel(picker, Strings.LabelLanguage);

            UIHelperBase diagnostics = helper.AddGroup(Loc.Get(Strings.GroupDiagnostics));

            UIButton testLog = Track(diagnostics.AddButton(Loc.Get(Strings.ButtonTestLog),
                                     Guarded("diagnostics", OnDiagnosticsButton)) as UIButton,
                                     Strings.ButtonTestLog);
            TrackGroupTitle(testLog, Strings.GroupDiagnostics);

            Track(diagnostics.AddButton(Loc.Get(Strings.ButtonBusPrefabs),
                  Guarded("bus prefab dump", BusPrefabResolver.DumpBusPrefabs)) as UIButton,
                  Strings.ButtonBusPrefabs);

            // Read-only, so it is not behind the spike arming checkbox. It does cost a
            // brief hitch: every phase is bounded, but they all run in one simulation
            // step because AddAction cannot split work across frames.
            Track(diagnostics.AddButton(Loc.Get(Strings.ButtonMeasure),
                  Guarded("performance probe", PerformanceProbe.Run)) as UIButton,
                  Strings.ButtonMeasure);

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

            UIHelperBase spike = helper.AddGroup(Loc.Get(Strings.GroupSpike));

            UICheckBox arm = Track(spike.AddCheckbox(Loc.Get(Strings.CheckSpikeArm),
                                   false, OnSpikeArmedChanged) as UICheckBox,
                                   Strings.CheckSpikeArm);
            TrackGroupTitle(arm, Strings.GroupSpike);

            Track(spike.AddButton(Loc.Get(Strings.ButtonSpikeCreate),
                  Guarded("create test line", ArmedOnly(SaveRoundTripSpike.CreateTestLine))) as UIButton,
                  Strings.ButtonSpikeCreate);
            Track(spike.AddButton(Loc.Get(Strings.ButtonSpikeReport),
                  Guarded("report test lines", SaveRoundTripSpike.ReportTestLines)) as UIButton,
                  Strings.ButtonSpikeReport);
            Track(spike.AddButton(Loc.Get(Strings.ButtonSpikeRemove),
                  Guarded("remove test lines", ArmedOnly(SaveRoundTripSpike.RemoveTestLines))) as UIButton,
                  Strings.ButtonSpikeRemove);
        }

        // ------------------------------------------------------------------ language

        private static void OnLanguageChanged(int selection)
        {
            string[] order = Loc.Languages;
            if (selection < 0 || selection >= order.Length)
            {
                return;
            }

            Loc.SetLanguage(order[selection]);
            Retranslate();
        }

        /// <summary>
        /// Rewrites every tracked label in the language now selected.
        ///
        /// Components are compared against null through Unity's own operator, which
        /// reports a destroyed object as null, so a panel torn down between the change
        /// and this call is skipped rather than throwing.
        /// </summary>
        private static void Retranslate()
        {
            for (int i = 0; i < s_localised.Count; i++)
            {
                string text = Loc.Get(s_localised[i].Value);
                object component = s_localised[i].Key;

                // UIButton and UILabel both take their text from UITextComponent.
                UITextComponent textComponent = component as UITextComponent;
                if (textComponent != null)
                {
                    textComponent.text = text;
                    continue;
                }

                // UICheckBox descends straight from UIComponent and has its own.
                UICheckBox checkBox = component as UICheckBox;
                if (checkBox != null)
                {
                    checkBox.text = text;
                }
            }
        }

        private static T Track<T>(T component, string key) where T : UIComponent
        {
            if (component != null)
            {
                s_localised.Add(new KeyValuePair<object, string>(component, key));
            }

            return component;
        }

        /// <summary>
        /// A dropdown's caption is a sibling UILabel named "Label", inside the panel
        /// the template created. UIHelper.AddDropdown sets it the same way.
        /// </summary>
        private static void TrackDropdownLabel(UIDropDown dropdown, string key)
        {
            if (dropdown == null || dropdown.parent == null)
            {
                return;
            }

            Track(dropdown.parent.Find<UILabel>("Label"), key);
        }

        /// <summary>
        /// A group's title is a UILabel named "Label" on the group panel, but AddGroup
        /// hands back a helper over the panel's "Content" child, and the root behind
        /// that helper is private. Climbing from a control inside the group reaches the
        /// same label without touching anything private: control -> Content -> group.
        /// </summary>
        private static void TrackGroupTitle(UIComponent childOfGroup, string key)
        {
            if (childOfGroup == null || childOfGroup.parent == null || childOfGroup.parent.parent == null)
            {
                return;
            }

            Track(childOfGroup.parent.parent.Find<UILabel>("Label"), key);
        }

        // ------------------------------------------------------------------ spike arming

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
