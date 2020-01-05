using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Core.Logging;
using T3.Gui.Graph;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public class UserSettings
    {
        public UserSettings()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            TryLoadingSettings();
        }

        public class ConfigData
        {
            public readonly Dictionary<Guid, ScalableCanvas.CanvasProperties> OperatorViewSettings = new Dictionary<Guid, ScalableCanvas.CanvasProperties>();
            public readonly Dictionary<string, Guid> LastOpsForWindows = new Dictionary<string, Guid>();
            [JsonConverter(typeof(StringEnumConverter))]
            public GraphCanvas.HoverModes HoverMode = GraphCanvas.HoverModes.Live;
            public bool AudioMuted;
        }

        public static ConfigData Config = new ConfigData();

        
        void OnProcessExit(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private static void SaveSettings()
        {
            Log.Debug("Saving user settings...");
            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(UserSettingFilepath))
            {
                serializer.Serialize(file, Config);
            }
        }

        private static void TryLoadingSettings()
        {
            if (!File.Exists(UserSettingFilepath))
            {
                Log.Warning($"Layout {UserSettingFilepath} doesn't exist yet");
                return;
            }

            var jsonBlob = File.ReadAllText(UserSettingFilepath);
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            if (!(serializer.Deserialize(fileTextReader, typeof(ConfigData))
                      is ConfigData configurations))
            {
                Log.Error("Can't load layout");
                return;
            }

            Config = configurations;
        }


        public static Guid GetLastOpenOpForWindow(string windowTitle)
        {
            return Config.LastOpsForWindows.ContainsKey(windowTitle)
                       ? Config.LastOpsForWindows[windowTitle]
                       : Guid.Empty;
        }
        
        public static void SaveLastViewedOpForWindow(GraphWindow window, Guid opInstanceId)
        {
            Config.LastOpsForWindows[window.Config.Title]= opInstanceId;
        }
        private const string UserSettingFilepath = "userSettings.json";
    }
}