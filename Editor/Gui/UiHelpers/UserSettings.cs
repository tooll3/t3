using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.IO;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.TimeLine;

namespace T3.Editor.Gui.UiHelpers
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
            public readonly Dictionary<Guid, ScalableCanvas.Scope> OperatorViewSettings = new();
            public readonly Dictionary<string, Guid> LastOpsForWindows = new();

            [JsonConverter(typeof(StringEnumConverter))]
            public GraphCanvas.HoverModes HoverMode = GraphCanvas.HoverModes.LastValue;

            public bool AudioMuted;
            
            // UI-Elements
            public bool ShowThumbnails = true;
            public bool ShowMainMenu = true;
            public bool ShowTitleAndDescription = true;
            public bool ShowToolbar = true;
            public bool ShowTimeline = true;
            public bool ShowMiniMap = false;
            public bool ShowInteractionOverlay = false;
            
            // UI-State
            public float UiScaleFactor = 1;
            public bool FullScreen = false;
            public bool FocusMode = false;
            public int WindowLayoutIndex = 0;
            public bool EnableIdleMotion = true;
            public bool SuspendRenderingWhenHidden = true;
            
            // Interaction
            public bool WarnBeforeLibEdit = true;
            public bool SmartGroupDragging = false;
            public bool ShowExplicitTextureFormatInOutputWindow = false;
            public bool UseArcConnections = true;
            public bool ResetTimeAfterPlayback;
            public float SnapStrength = 5;
            public ValueEditMethods ValueEditMethod;
            public float ScrollSmoothing = 0.1f;

            public float ClickThreshold = 5; // Increase for high-res display and pen tablets
            public bool AdjustCameraSpeedWithMouseWheel = false;
            public float CameraSpeed = 1;

            public bool MiddleMouseButtonZooms = false;

            public TimeLineCanvas.FrameStepAmount FrameStepAmount = TimeLineCanvas.FrameStepAmount.FrameAt30Fps;
            
            public bool MouseWheelEditsNeedCtrlKey = true;
            public bool AutoPinAllAnimations = false;

            
            public float KeyboardScrollAcceleration = 2.5f;

            public bool VariationLiveThumbnails = true;
            public bool VariationHoverPreview = true;

            public bool EditorHoverPreview = true;
            
            // Load Save
            public string UserName = UndefinedUserName;
            public bool EnableAutoBackup = true;

            // Other settings
            public float GizmoSize = 100;
            public int FullScreenIndexMain = 0;
            public int FullScreenIndexViewer = 0;
            


            // Timeline
            public float TimeRasterDensity = 1f;

            // Space mouse
            public float SpaceMouseRotationSpeedFactor = 1f;
            public float SpaceMouseMoveSpeedFactor = 1f;
            public float SpaceMouseDamping = 0.5f;
            

            // Rendering (controlled from render windows)
            public string RenderVideoFilePath = "./Render/render-v01.mp4";
            public string RenderSequenceFilePath = "./ImageSequence/";

            // Profiling
            public bool EnableFrameProfiling = true;
            public bool KeepTraceForLogMessages = false;
            public bool EnableGCProfiling = false;

            
            [JsonConverter(typeof(StringEnumConverter))]
            public TimeFormat.TimeDisplayModes TimeDisplayMode = TimeFormat.TimeDisplayModes.Bars;
            
            public List<GraphBookmarkNavigation.Bookmark> Bookmarks = new();
            public List<Gradient> GradientPresets = new();

            public string ColorThemeName;
        }

        public enum ValueEditMethods
        {
            InfinitySlider,
            RadialSlider,
            JogDial,
            ValueLadder,
        }
        
        public static bool IsUserNameDefined()
        {
            return !string.IsNullOrEmpty(Config.UserName) && Config.UserName != UndefinedUserName;
        }

        private const string UndefinedUserName = "unknown";

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