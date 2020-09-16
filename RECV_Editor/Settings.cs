using System;
using System.IO;
using System.Web.Script.Serialization;

namespace RECV_Editor
{
    class Settings
    {
        public SettingsData Data { get; private set; }

        readonly string settingsFileName;

        public Settings(string settingsFileName)
        {
            if (string.IsNullOrEmpty(settingsFileName))
            {
                throw new ArgumentNullException(nameof(settingsFileName));
            }

            this.settingsFileName = settingsFileName;

            Data = new SettingsData();
        }

        public bool Load()
        {
            if (!File.Exists(settingsFileName))
            {
                Logger.Append("Settings file not found. Initializing settings...");
                return false;
            }
            else
            {
                Logger.Append("Loading settings file...");

                JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
                Data = jsSerializer.Deserialize<SettingsData>(File.ReadAllText(settingsFileName));

                if (!Data.CheckIfValid())
                {
                    Logger.Append("Settings file contains invalid entries. Initializing settings...");
                    return false;
                }
            }

            Logger.Append("Settings have been loaded.");
            return true;
        }

        public void Save()
        {
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
            File.WriteAllText(settingsFileName, jsSerializer.Serialize(Data));
        }

        public class SettingsData
        {
            public string OriginalGameRootFolder { get; set; }
            public string GeneratedGameRootFolder { get; set; }
            public string ProjectFolder { get; set; }
            public string TableFile { get; set; }

            public bool CheckIfValid()
            {
                if (!Directory.Exists(OriginalGameRootFolder)) return false;
                if (!Directory.Exists(GeneratedGameRootFolder)) return false;
                if (!Directory.Exists(ProjectFolder)) return false;
                if (!File.Exists(TableFile)) return false;

                return true;
            }
        }
    }
}