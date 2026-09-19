using System.Collections.Generic;
using ColossalFramework.Globalization;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Looks up a user-facing string in the language the player has the game set to.
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
        /// <summary>
        /// Language the tables are written in, and the fallback for everything else.
        /// Matches LocaleManager.defaultLanguage.
        /// </summary>
        public const string Fallback = "en";

        /// <summary>
        /// Maps the game's locale id onto one of our translation sets.
        ///
        /// The game itself ships nine locale files — de, en, es, fr, ko, pl, pt, ru, zh
        /// (verified from Files\Locale\*.locale). LocaleManager builds its supported
        /// list by reading that directory at runtime, so a workshop translation can add
        /// its own file and produce an id that is not in that nine. Japanese and
        /// Traditional Chinese only ever arrive that way.
        ///
        /// The ids for those added locales are therefore candidates, not verified
        /// facts: the entries below cover the spellings such mods commonly use, and
        /// anything unrecognised falls back to English rather than guessing.
        /// </summary>
        private static readonly Dictionary<string, string> LocaleIdMap = BuildLocaleIdMap();

        private static readonly Dictionary<string, Dictionary<string, string>> Tables = Strings.BuildTables();

        private static Dictionary<string, string> BuildLocaleIdMap()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            // Shipped with the game.
            map["en"] = "en";
            map["de"] = "de";
            map["es"] = "es";
            map["fr"] = "fr";
            map["ko"] = "ko";
            map["pl"] = "pl";
            map["pt"] = "pt-BR";
            map["ru"] = "ru";

            // The game ships a single Chinese locale called "zh".
            // UNVERIFIED which variant it is: the .locale files are packed, so this
            // could not be read from disk. Simplified is the assumption because that is
            // what the official localisation is generally understood to be. If the game
            // turns out to ship Traditional, this one line is the whole fix.
            map["zh"] = "zh-Hans";

            // Added by workshop translations. Spellings are not verified.
            map["ja"] = "ja";
            map["jp"] = "ja";
            map["zh-cn"] = "zh-Hans";
            map["zh-hans"] = "zh-Hans";
            map["chs"] = "zh-Hans";
            map["zh-tw"] = "zh-Hant";
            map["zh-hk"] = "zh-Hant";
            map["zh-hant"] = "zh-Hant";
            map["cht"] = "zh-Hant";

            return map;
        }

        /// <summary>
        /// The translation set in use, as one of our own language tags.
        /// </summary>
        public static string CurrentLanguage
        {
            get { return ResolveLanguage(); }
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
            if (Tables.TryGetValue(ResolveLanguage(), out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            if (Tables.TryGetValue(Fallback, out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            return key;
        }

        /// <summary>
        /// Reads the game's current locale and maps it onto one of our tables.
        ///
        /// LocaleManager derives from SingletonLite, whose instance property CREATES
        /// the singleton when it is null rather than returning null. Touching it too
        /// early would therefore manufacture a LocaleManager whose current code is
        /// empty — a fabricated answer rather than a missing one. The exists check is
        /// what keeps that from happening.
        ///
        /// Resolved on every call rather than cached: the player can change language
        /// from the options screen at any time, and every caller here is building UI,
        /// where a dictionary lookup costs nothing worth measuring. Caching would mean
        /// subscribing to the static eventLocaleChanged and owning an unsubscribe for
        /// the lifetime of the process, which is a great deal more to get wrong.
        /// </summary>
        private static string ResolveLanguage()
        {
            if (!LocaleManager.exists)
            {
                return Fallback;
            }

            string code = LocaleManager.instance.language;
            if (string.IsNullOrEmpty(code))
            {
                return Fallback;
            }

            code = code.ToLowerInvariant().Replace('_', '-');

            string mapped;
            if (LocaleIdMap.TryGetValue(code, out mapped))
            {
                return mapped;
            }

            // "pt-br" and the like: fall back on the part before the separator so a
            // regional variant still finds its base language.
            int dash = code.IndexOf('-');
            if (dash > 0 && LocaleIdMap.TryGetValue(code.Substring(0, dash), out mapped))
            {
                return mapped;
            }

            return Fallback;
        }
    }
}
