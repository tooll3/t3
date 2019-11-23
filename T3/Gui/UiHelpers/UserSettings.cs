using System;
using System.Collections.Generic;
using System.IO;
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
            public Dictionary<Guid, ScalableCanvas.CanvasProperties> OperatorViewSettings = new Dictionary<Guid, ScalableCanvas.CanvasProperties>();
            public Dictionary<string, Guid> LastOpsForWindows = new Dictionary<string, Guid>();


        }

        public static ConfigData Config= new ConfigData();

        
        void OnProcessExit(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private static void SaveSettings()
        {
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var writer = new StringWriter();
            serializer.Serialize(writer, Config);

            var file = File.CreateText(UserSettingFilepath);
            file.Write(writer.ToString());
            file.Close();
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

        private const string UserSettingFilepath = "userSettings.json";

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
    }
}