#nullable enable
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.IO;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.TimeLine;

namespace T3.Editor.Gui.UiHelpers;

/// <summary>
/// Saves view layout, currently open node and other user settings 
/// </summary>
///  todo - make internal, make extendable by external packaages
public sealed class UserSettings : Settings<UserSettings.ConfigData>
{
    internal UserSettings(bool saveOnQuit) : base("userSettings.json", saveOnQuit:saveOnQuit)
    {
    }
    
    public sealed class ConfigData
    {
        internal readonly Dictionary<Guid, CanvasScope> OperatorViewSettings = new();
        internal readonly Dictionary<string, Guid> LastOpsForWindows = new();

        [JsonConverter(typeof(StringEnumConverter))]
        internal GraphHoverModes HoverMode = GraphHoverModes.LastValue;

        internal bool AudioMuted;
            
        // UI-Elements
        internal bool ShowThumbnails = true;
        internal bool ShowMainMenu = true;
        internal bool ShowTitleAndDescription = true;
        internal bool ShowToolbar = true;
        internal bool ShowTimeline = true;
        internal bool ShowMiniMap = false;
        internal bool ShowInteractionOverlay = false;
            
        // UI-State
        internal float UiScaleFactor = 1;
        internal bool FullScreen = false;
        internal bool FocusMode = false;
        internal int WindowLayoutIndex = 0;
        internal bool EnableIdleMotion = true;
        internal bool SuspendRenderingWhenHidden = true;
        internal bool MirrorUiOnSecondView = false;
            
        // Interaction
        internal bool WarnBeforeLibEdit = true;
        internal bool SmartGroupDragging = false;
        internal bool DisconnectOnUnsnap = false;
        
        internal readonly bool ShowExplicitTextureFormatInOutputWindow = false;
        internal bool UseArcConnections = true;
        internal bool ResetTimeAfterPlayback;
        internal float SnapStrength = 5;
        internal ValueEditMethods ValueEditMethod;
        internal float ScrollSmoothing = 0.1f;
        internal float MaxCurveRadius = 150;

        public float ClickThreshold = 5; // Increase for high-res display and pen tablets
        internal bool AdjustCameraSpeedWithMouseWheel = false;
        internal float CameraSpeed = 1;

        internal bool MiddleMouseButtonZooms = false;

        internal FrameStepAmount FrameStepAmount = FrameStepAmount.FrameAt30Fps;
            
        internal bool MouseWheelEditsNeedCtrlKey = true;
        internal bool AutoPinAllAnimations = false;

            
        internal readonly float KeyboardScrollAcceleration = 2.5f;

        internal bool VariationLiveThumbnails = true;
        internal bool VariationHoverPreview = true;

        internal bool EditorHoverPreview = true;
            
        // Load Save
        internal string UserName = UndefinedUserName;
        internal bool EnableAutoBackup = true;

        // Other settings
        internal float GizmoSize = 100;
        internal int FullScreenIndexMain = 0;
        internal int FullScreenIndexViewer = 0;
            


        // Timeline
        internal float TimeRasterDensity = 1f;

        // Space mouse
        internal float SpaceMouseRotationSpeedFactor = 1f;
        internal float SpaceMouseMoveSpeedFactor = 1f;
        internal float SpaceMouseDamping = 0.5f;
            

        // Rendering (controlled from render windows)
        internal string RenderVideoFilePath = "./Render/render-v01.mp4";
        internal string RenderSequenceFilePath = "./ImageSequence/";

        // Profiling
        internal bool EnableFrameProfiling = true;
        internal bool KeepTraceForLogMessages = false;
        internal bool EnableGCProfiling = false;

            
        [JsonConverter(typeof(StringEnumConverter))]
        internal TimeFormat.TimeDisplayModes TimeDisplayMode = TimeFormat.TimeDisplayModes.Bars;
            
        internal readonly List<Bookmark> Bookmarks = [];
        internal List<Gradient> GradientPresets = [];

        internal string ColorThemeName = string.Empty;
            
        internal bool ExpandSpectrumVisualizerVertically = true;
            
        private string _defaultNewProjectDirectory = _defaultProjectFolder;
        internal string DefaultNewProjectDirectory => _defaultNewProjectDirectory ??= _defaultProjectFolder;

        private static readonly string _defaultProjectFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T3Projects");
    }

    internal enum ValueEditMethods
    {
        InfinitySlider,
        RadialSlider,
        JogDial,
        ValueLadder,
    }
    
    internal enum GraphHoverModes
    {
        Disabled,
        Live,
        LastValue,
    }
        
    internal static bool IsUserNameDefined()
    {
        return !string.IsNullOrEmpty(Config.UserName) && Config.UserName != UndefinedUserName;
    }

    private const string UndefinedUserName = "unknown";

    internal static Guid GetLastOpenOpForWindow(string windowTitle)
    {
        return Config.LastOpsForWindows.TryGetValue(windowTitle, out var id) ? id : Guid.Empty;
    }

    internal static void SaveLastViewedOpForWindow(string title, Guid opInstanceId)
    {
        Config.LastOpsForWindows[title] = opInstanceId;
    }
}