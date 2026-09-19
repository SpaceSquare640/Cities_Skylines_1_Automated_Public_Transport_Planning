using System;
using System.IO;
using System.Xml.Serialization;
using ColossalFramework.IO;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Settings the player chooses, kept in an XML file next to the ones every other
    /// mod writes (DataLocation.localApplicationData).
    ///
    /// Reading is always safe. A file that is missing, unreadable, or written by a
    /// future version yields defaults rather than an exception — a settings file is
    /// never a good enough reason to stop a mod from loading.
    /// </summary>
    [XmlRoot("AutomatedPublicTransportPlanning")]
    public sealed class Settings
    {
        private const string FileName = "AutomatedPublicTransportPlanning.xml";

        /// <summary>
        /// The language the player picked, as one of the tags in Strings.BuildTables.
        ///
        /// The game's own language is deliberately NOT consulted. A player running the
        /// game in one language may well want this panel in another, and silently
        /// following the game would override a choice they made on purpose.
        /// </summary>
        public string Language = Loc.Fallback;

        private static Settings s_current;

        public static Settings Current
        {
            get
            {
                if (s_current == null)
                {
                    s_current = Load();
                }

                return s_current;
            }
        }

        private static string FilePath
        {
            get { return Path.Combine(DataLocation.localApplicationData, FileName); }
        }

        private static Settings Load()
        {
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                {
                    return new Settings();
                }

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serialiser = new XmlSerializer(typeof(Settings));
                    Settings loaded = serialiser.Deserialize(stream) as Settings;
                    if (loaded == null)
                    {
                        return new Settings();
                    }

                    // A tag this build does not know about — an older or newer version,
                    // or a hand-edited file. Fall back rather than leaving the panel
                    // showing nothing but raw lookup keys.
                    if (!Loc.HasLanguage(loaded.Language))
                    {
                        Log.Warning("Settings name a language this build does not have ('" +
                                    loaded.Language + "'); using " + Loc.Fallback + ".");
                        loaded.Language = Loc.Fallback;
                    }

                    return loaded;
                }
            }
            catch (Exception e)
            {
                Log.Exception("Could not read settings; using defaults", e);
                return new Settings();
            }
        }

        public void Save()
        {
            try
            {
                using (FileStream stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                {
                    XmlSerializer serialiser = new XmlSerializer(typeof(Settings));
                    serialiser.Serialize(stream, this);
                }
            }
            catch (Exception e)
            {
                // Losing the choice on the next start is a far smaller problem than
                // throwing out of a UI event handler.
                Log.Exception("Could not write settings", e);
            }
        }
    }
}
