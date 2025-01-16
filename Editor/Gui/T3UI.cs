using System.Diagnostics;
using System.Threading.Tasks;
using T3.Editor.Gui.Graph;
using ImGuiNET;
using Operators.Utils.Recording;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.DataSet;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Dialog;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Midi;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Templates;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.Gui.Windows.RenderExport;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui;

public static class T3Ui
{
    internal static void InitializeEnvironment()
    {
        //WindowManager.TryToInitialize();
        ExampleSymbolLinking.UpdateExampleLinks();

        Playback.Current = DefaultTimelinePlayback;
        ThemeHandling.Initialize();
    }

    internal static readonly Playback DefaultTimelinePlayback = new();
    internal static readonly BeatTimingPlayback DefaultBeatTimingPlayback = new();

    private static void InitializeAfterAppWindowReady()
    {
        if (_initialed || ImGui.GetWindowSize() == Vector2.Zero)
            return;


        CompatibleMidiDeviceHandling.InitializeConnectedDevices();
        _initialed = true;
    }

    private static bool _initialed;

    internal static void ProcessFrame()
    {
        Profiling.KeepFrameData();
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
        DragHandling.Update();

        CustomComponents.BeginFrame();
        FormInputs.BeginFrame();
        InitializeAfterAppWindowReady();

        // Prepare the current frame 
        RenderStatsCollector.StartNewFrame();
            
        if (!Playback.Current.IsRenderingToFile && GraphWindow.Focused != null)
        {
            PlaybackUtils.UpdatePlaybackAndSyncing();
            AudioEngine.CompleteFrame(Playback.Current, Playback.LastFrameDuration);    // Update
        }
        TextureReadAccess.Update();

        AutoBackup.AutoBackup.IsEnabled = UserSettings.Config.EnableAutoBackup;
        
        ResourceManager.RaiseFileWatchingEvents();

        VariationHandling.Update();
        MouseWheelFieldWasHoveredLastFrame = MouseWheelFieldHovered;
        MouseWheelFieldHovered = false;

        // A work around for potential mouse capture
        DragFieldWasHoveredLastFrame = DragFieldHovered;
        DragFieldHovered = false;
        
        FitViewToSelectionHandling.ProcessNewFrame();
        SrvManager.RemoveForDisposedTextures();
        KeyboardBinding.InitFrame();
        ConnectionSnapEndHelper.PrepareNewFrame();
        CompatibleMidiDeviceHandling.UpdateConnectedDevices();

        var nodeSelection = GraphWindow.Focused?.Components.NodeSelection;

        // Set selected id so operator can check if they are selected or not  
        var selectedInstance = nodeSelection?.GetSelectedInstanceWithoutComposition();
        MouseInput.SelectedChildId = selectedInstance?.SymbolChildId ?? Guid.Empty;

        if (nodeSelection != null)
        {
            InvalidateSelectedOpsForTransormGizmo(nodeSelection);
        }

        // Draw everything!
        ImGui.DockSpaceOverViewport();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        WindowManager.Draw();
        ImGui.PopStyleVar();
            
        // Complete frame
        SingleValueEdit.StartNextFrame();
        SelectableNodeMovement.CompleteFrame();
        
        FrameStats.CompleteFrame();
        TriggerGlobalActionsFromKeyBindings();

        if (UserSettings.Config.ShowMainMenu || ImGui.GetMousePos().Y < 20)
        {
            AppMenu.DrawAppMenuBar();
        }
            
        _searchDialog.Draw();
        NewProjectDialog.Draw();
        CreateFromTemplateDialog.Draw();
        _userNameDialog.Draw();

        if (IsWindowLayoutComplete())
        {
            if (!UserSettings.IsUserNameDefined() )
            {
                UserSettings.Config.UserName = Environment.UserName;
                _userNameDialog.ShowNextFrame();
            }
        }

        KeyboardAndMouseOverlay.Draw();

        Playback.OpNotReady = false;
        AutoBackup.AutoBackup.CheckForSave();

        Profiling.EndFrameData();
    }

    private static void InvalidateSelectedOpsForTransormGizmo(NodeSelection nodeSelection)
    {
        // Keep invalidating selected op to enforce rendering of Transform gizmo  
        foreach (var si in nodeSelection.GetSelectedInstances().ToList())
        {
            if (si is not ITransformable)
                continue;

            foreach (var i in si.Inputs)
            {
                // Skip string inputs to prevent potential interference with resource file paths hooks
                // I.e. Invalidating these every frame breaks shader recompiling if Shader-op is selected
                if (i.ValueType == typeof(string))
                {
                    continue;
                }

                i.DirtyFlag.Invalidate();
            }
        }
    }

    /// <summary>
    /// This a bad workaround to defer some ui actions until we have completed all
    /// window initialization, so they are not discarded by the setup process.
    /// </summary>
    private static bool IsWindowLayoutComplete() => ImGui.GetFrameCount() > 2;

    private static void TriggerGlobalActionsFromKeyBindings()
    {
        if (KeyboardBinding.Triggered(UserActions.Undo))
        {
            UndoRedoStack.Undo();
        }
        else if (KeyboardBinding.Triggered(UserActions.Redo))
        {
            UndoRedoStack.Redo();
        }
        else if (KeyboardBinding.Triggered(UserActions.Save))
        {
            SaveInBackground(saveAll: false);
        }
        else if (KeyboardBinding.Triggered(UserActions.ToggleAllUiElements))
        {
            ToggleAllUiElements();
        }
        else if (KeyboardBinding.Triggered(UserActions.SearchGraph))
        {
            _searchDialog.ShowNextFrame();
        }
        else if (KeyboardBinding.Triggered(UserActions.ToggleFullscreen))
        {
            UserSettings.Config.FullScreen = !UserSettings.Config.FullScreen;
        }
        else if (KeyboardBinding.Triggered(UserActions.ToggleFocusMode)) ToggleFocusMode();
    }

    public static void ToggleFocusMode()
    {
        var shouldBeFocusMode = !UserSettings.Config.FocusMode;

        var outputWindow = OutputWindow.GetPrimaryOutputWindow();
        var primaryGraphWindow = GraphWindow.Focused;

        if (shouldBeFocusMode && outputWindow != null && primaryGraphWindow != null)
        {
            outputWindow.Pinning.TryGetPinnedOrSelectedInstance(out var instance, out _);
            primaryGraphWindow.GraphImageBackground.OutputInstance = instance;
        }

        UserSettings.Config.FocusMode = shouldBeFocusMode;
        UserSettings.Config.ShowToolbar = shouldBeFocusMode;
        ToggleAllUiElements();
        LayoutHandling.LoadAndApplyLayoutOrFocusMode(shouldBeFocusMode ? 11 : UserSettings.Config.WindowLayoutIndex);

        outputWindow = OutputWindow.GetPrimaryOutputWindow();
        if (!shouldBeFocusMode && outputWindow != null && primaryGraphWindow != null)
        {
            outputWindow.Pinning.PinInstance(primaryGraphWindow.GraphImageBackground.OutputInstance, primaryGraphWindow.Components);
            primaryGraphWindow.GraphImageBackground.ClearBackground();
        }
    }

    public static void ToggleAllUiElements()
    {
        //T3Ui.MaximalView = !T3Ui.MaximalView;
        if (UserSettings.Config.ShowToolbar)
        {
            UserSettings.Config.ShowMainMenu = false;
            UserSettings.Config.ShowTitleAndDescription = false;
            UserSettings.Config.ShowToolbar = false;
            UserSettings.Config.ShowTimeline = false;
        }
        else
        {
            UserSettings.Config.ShowMainMenu = true;
            UserSettings.Config.ShowTitleAndDescription = true;
            UserSettings.Config.ShowToolbar = true;
            if (Playback.Current.Settings.Syncing == PlaybackSettings.SyncModes.Timeline)
            {
                UserSettings.Config.ShowTimeline = true;
            }
        }
    }

    public static void SaveInBackground(bool saveAll)
    {
        Task.Run(() => Save(saveAll));
    }

    internal static void Save(bool saveAll)
    {
        if (_saveStopwatch.IsRunning)
        {
            Log.Debug("Can't save modified while saving is in progress");
            return;
        }

        _saveStopwatch.Restart();

        // Todo - parallelize? 
        foreach (var package in EditableSymbolProject.AllProjects)
        {
            if (saveAll)
                package.SaveAll();
            else
                package.SaveModifiedSymbols();
        }

        _saveStopwatch.Stop();
    }

    internal static void SelectAndCenterChildIdInView(Guid symbolChildId)
    {
        var primaryGraphWindow = GraphWindow.Focused;
        if (primaryGraphWindow == null)
            return;

        var components = primaryGraphWindow.Components;
        var compositionOp = components.CompositionOp;

        var symbolUi = compositionOp.GetSymbolUi();
        
        if(!symbolUi.ChildUis.TryGetValue(symbolChildId, out var sourceChildUi))
            return;
        
        if(!compositionOp.Children.TryGetValue(symbolChildId, out var selectionTargetInstance))
            return;
        
        components.NodeSelection.SetSelection(sourceChildUi, selectionTargetInstance);
        FitViewToSelectionHandling.FitViewToSelection();
    }

    // private static void SwapHoveringBuffers()
    // {
    //     (HoveredIdsLastFrame, _hoveredIdsForNextFrame) = (_hoveredIdsForNextFrame, HoveredIdsLastFrame);
    //     _hoveredIdsForNextFrame.Clear();
    //     
    //     (RenderedIdsLastFrame, _renderedIdsForNextFrame) = (_renderedIdsForNextFrame, RenderedIdsLastFrame);
    //     _renderedIdsForNextFrame.Clear();            
    // }

    /// <summary>
    /// Statistics method for debug purpose
    /// </summary>
    private static void CountSymbolUsage()
    {
        var counts = new Dictionary<Symbol, int>();
        foreach (var s in EditorSymbolPackage.AllSymbols)
        {
            foreach (var child in s.Children.Values)
            {
                counts.TryAdd(child.Symbol, 0);
                counts[child.Symbol]++;
            }
        }

        foreach (var (s, c) in counts.OrderBy(c => counts[c.Key]).Reverse())
        {
            Log.Debug($"{s.Name} - {s.Namespace}  {c}");
        }
    }
    

    //@imdom: needs clarification how to handle osc data disconnection on shutdown
    // public void Dispose()
    // {
    //     GC.SuppressFinalize(this);
    //     OscDataRecording.Dispose();
    // }
    
    internal static bool DraggingIsInProgress = false;
    internal static bool MouseWheelFieldHovered { private get; set; }
    internal static bool MouseWheelFieldWasHoveredLastFrame { get; private set; }
    internal static bool DragFieldHovered { private get; set; }
    internal static bool DragFieldWasHoveredLastFrame { get; private set; }
    
    internal static bool ShowSecondaryRenderWindow => WindowManager.ShowSecondaryRenderWindow;
    internal const string FloatNumberFormat = "{0:F2}";

    private static readonly Stopwatch _saveStopwatch = new();

    // ReSharper disable once InconsistentlySynchronizedField
    internal static bool IsCurrentlySaving => _saveStopwatch is { IsRunning: true };

    public static float UiScaleFactor { get; internal set; } = 1;
    internal static float DisplayScaleFactor { get; set; } = 1;
    internal static bool IsAnyPopupOpen => !string.IsNullOrEmpty(FrameStats.Last.OpenedPopUpName);

    internal static readonly MidiDataRecording MidiDataRecording = new(DataRecording.ActiveRecordingSet);
    internal static readonly OscDataRecording OscDataRecording = new(DataRecording.ActiveRecordingSet);

    //private static readonly AutoBackup.AutoBackup _autoBackup = new();

    internal static readonly CreateFromTemplateDialog CreateFromTemplateDialog = new();
    private static readonly UserNameDialog _userNameDialog = new();
    private static readonly SearchDialog _searchDialog = new();
    internal static readonly NewProjectDialog NewProjectDialog = new();

    [Flags]
    public enum EditingFlags
    {
        None = 0,
        ExpandVertically = 1 << 1,
        PreventMouseInteractions = 1 << 2,
        PreventZoomWithMouseWheel = 1 << 3,
        PreventPanningWithMouse = 1 << 4,
        AllowHoveredChildWindows = 1 << 5,
    }

    internal static bool UseVSync = true;
    public static bool ItemRegionsVisible;
    

}