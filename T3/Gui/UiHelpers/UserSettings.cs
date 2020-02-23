using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Gui.Graph;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout, currently open node and other usersettings 
    /// </summary>
    public class UserSettings :Settings
    {
        static UserSettings()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Config = TryLoading<ConfigData>("userSettings.json") ?? new ConfigData();
        }
        
        public static ConfigData Config;
        
        public class ConfigData
        {
            public readonly Dictionary<Guid, ScalableCanvas.CanvasProperties> OperatorViewSettings = new Dictionary<Guid, ScalableCanvas.CanvasProperties>();
            public readonly Dictionary<string, Guid> LastOpsForWindows = new Dictionary<string, Guid>();
            [JsonConverter(typeof(StringEnumConverter))]
            public GraphCanvas.HoverModes HoverMode = GraphCanvas.HoverModes.Live;
            public bool AudioMuted;
            public bool ShowThumbnails = true;
            public int WindowLayoutIndex = 0;
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

        private static void OnProcessExit(object sender, EventArgs e)
        {
            SaveSettings(Config);
        }
    }
}