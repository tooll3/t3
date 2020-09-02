using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Gui.Graph;
using T3.Gui.Interaction;
using T3.Gui.Windows;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout, currently open node and other user settings 
    /// </summary>
    public class UserSettings : Settings<UserSettings.ConfigData>
    {
        public UserSettings() : base("userSettings.json")
        {
        }

        public class ConfigData
        {
            public readonly Dictionary<Guid, ScalableCanvas.Scope> OperatorViewSettings = new Dictionary<Guid, ScalableCanvas.Scope>();
            public readonly Dictionary<string, Guid> LastOpsForWindows = new Dictionary<string, Guid>();

            [JsonConverter(typeof(StringEnumConverter))]
            public GraphCanvas.HoverModes HoverMode = GraphCanvas.HoverModes.Live;

            public bool AudioMuted;
            public bool ShowThumbnails = true;
            public int WindowLayoutIndex = 0;
            public bool KeepBeatTimeRunningInPause = false;
            public bool ShowExplicitTextureFormatInOutputWindow = false;
            public bool UseArcConnections = false;
            public float SnapStrength = 5;
            public bool UseJogDialControl = false;
            public float ZoomSpeed = 12;
            public float TooltipDelay = 1.2f;
            public bool HideUiElementsInGraphWindow = false;
            public List<GraphBookmarkNavigation.Bookmark> Bookmarks = new List<GraphBookmarkNavigation.Bookmark>();
        }

        public static Guid GetLastOpenOpForWindow(string windowTitle)
        {
            return Config.LastOpsForWindows.TryGetValue(windowTitle, out var id) ? id : Guid.Empty;
        }

        public static void SaveLastViewedOpForWindow(GraphWindow window, Guid opInstanceId)
        {
            Config.LastOpsForWindows[window.Config.Title] = opInstanceId;
        }
    }

    
}