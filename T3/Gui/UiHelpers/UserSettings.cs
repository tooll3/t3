using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Core.Animation;
using T3.Core.IO;
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
        public UserSettings(bool saveOnQuit) : base("userSettings.json", saveOnQuit:saveOnQuit)
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
            public bool SmartGroupDragging = false;
            public int WindowLayoutIndex = 0;
            public bool KeepBeatTimeRunningInPause = true;
            public bool ShowExplicitTextureFormatInOutputWindow = false;
            public bool UseArcConnections = true;
            public float SnapStrength = 5;
            public bool UseJogDialControl = false;
            public float ScrollSmoothing = 0.06f;
            public float TooltipDelay = 1.2f;
            public bool HideUiElementsInGraphWindow = false;
            public float ClickThreshold = 5; // Increase for high-res display and pen tablets
            public float GizmoSize = 100;
            public bool AutoSaveAfterSymbolCreation = true;
            public bool EnableAutoBackup = true;
            public bool SaveOnlyModified = false;
            public bool SwapMainAnd2ndWindowsWhenFullscreen = true;
            public bool PresetsResetToDefaultValues = true;

            public float TimeRasterDensity = 1f;
            public bool CountBarsFromZero = true; 

            [JsonConverter(typeof(StringEnumConverter))]
            public Playback.TimeDisplayModes TimeDisplayMode = Playback.TimeDisplayModes.Bars;
            
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