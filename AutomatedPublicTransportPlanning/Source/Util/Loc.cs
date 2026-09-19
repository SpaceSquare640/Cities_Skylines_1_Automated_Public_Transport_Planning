using System.Collections.Generic;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Looks up a user-facing string in the language the player picked.
    ///
    /// The game's own language is deliberately not consulted. Following it would mean
    /// mapping the game's locale ids onto these tables, and those ids are not a fixed
    /// set: LocaleManager builds its list by reading the locale directory at runtime,
    /// so a workshop translation can introduce an id no version of this mod has heard
    /// of. Letting the player say what they want removes that whole problem, and it
    /// also lets someone run the game in one language and this panel in another.
    ///
    /// English is the source language and the fallback. A key with no translation for
    /// the current language falls back to English; a key with no English entry falls
    /// back to the key itself, so a missing string shows up as an obvious marker on
    /// screen rather than as an empty label.
    ///
    /// Log messages deliberately do NOT go through this. They are read by developers,
    /// often pasted into bug reports, and a log in a language the reader cannot search
    /// is worse than no log.
    /// </summary>
    public static class Loc
    {
        /// <summary>Language the tables are written in, and the fallback for everything else.</summary>
        public const string Fallback = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables = Strings.BuildTables();

        /// <summary>
        /// Language tags in the order they should appear in a picker, so that the
        /// dropdown and the stored setting cannot drift apart.
        /// </summary>
        public static string[] Languages
        {
            get { return Strings.LanguageOrder; }
        }

        /// <summary>Display names, in the same order as <see cref="Languages"/>.</summary>
        public static string[] LanguageNames
        {
            get { return Strings.LanguageNames; }
        }

        public static string CurrentLanguage
        {
            get
            {
                string tag = Settings.Current.Language;
                return HasLanguage(tag) ? tag : Fallback;
            }
        }

        public static bool HasLanguage(string tag)
        {
            return tag != null && Tables.ContainsKey(tag);
        }

        /// <summary>
        /// Index of the current language within <see cref="Languages"/>, for seeding a
        /// dropdown. Falls back to English's position rather than -1.
        /// </summary>
        public static int CurrentIndex
        {
            get
            {
                string tag = CurrentLanguage;
                string[] order = Strings.LanguageOrder;

                for (int i = 0; i < order.Length; i++)
                {
                    if (order[i] == tag)
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Stores the choice and writes it out. Callers are responsible for refreshing
        /// anything already on screen; nothing here reaches into the UI.
        /// </summary>
        public static void SetLanguage(string tag)
        {
            if (!HasLanguage(tag))
            {
                return;
            }

            if (Settings.Current.Language == tag)
            {
                return;
            }

            Settings.Current.Language = tag;
            Settings.Current.Save();
            Log.Info("Language set to '" + tag + "'.");
        }

        /// <summary>
        /// The translated string for a key, or an English one, or the key itself.
        /// Never returns null, so a caller can hand the result straight to a UI label.
        /// </summary>
        public static string Get(string key)
        {
            if (key == null)
            {
                return string.Empty;
            }

            string text;
            Dictionary<string, string> table;

            if (Tables.TryGetValue(CurrentLanguage, out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            if (Tables.TryGetValue(Fallback, out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            return key;
        }
    }
}
