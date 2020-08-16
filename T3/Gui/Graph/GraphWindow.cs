using System;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.TimeLine;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public partial class GraphWindow : Window
    {
        public GraphWindow()
        {
            _instanceCounter++;
            Config.Title = "Graph##" + _instanceCounter;
            Config.Visible = true;
            AllowMultipleInstances = true;

            _playback = File.Exists(ProjectSettings.Config.SoundtrackFilepath)
                            ? new StreamPlayback(ProjectSettings.Config.SoundtrackFilepath)
                            : new Playback();

            _playback.Bpm = ProjectSettings.Config.SoundtrackBpm;
            if (_playback is StreamPlayback streamPlayback)
                streamPlayback.SetMuteMode(UserSettings.Config.AudioMuted);

            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);
            var shownOpInstance = FindIdInNestedChildren(T3Ui.UiModel.RootInstance, opId) ?? T3Ui.UiModel.RootInstance;
            var path = NodeOperations.BuildIdPathForInstance(shownOpInstance);
            _graphCanvas = new GraphCanvas(this, path);

            _timeLineCanvas = new TimeLineCanvas(ref _playback);

            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            GraphWindowInstances.Add(this);
        }

        public static bool CanOpenAnotherWindow()
        {
            if (_instanceCounter > 0)
            {
                Log.Error("only one graph window supported for now");
                return false;
            }

            return true;
        }

        public static Instance FindIdInNestedChildren(Instance instance, Guid childId)
        {
            foreach (var child in instance.Children)
            {
                if (child.SymbolChildId == childId)
                {
                    return child;
                }

                var result = FindIdInNestedChildren(child, childId);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static int _instanceCounter;
        private static readonly List<Window> GraphWindowInstances = new List<Window>();

        public override List<Window> GetInstances()
        {
            return GraphWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
            _playback.Update(ImGui.GetIO().DeltaTime, UserSettings.Config.KeepBeatTimeRunningInPause);
        }

        private static GraphWindow _currentWindow;

        protected override void DrawAllInstances()
        {
            foreach (var w in GraphWindowInstances.ToArray())
            {
                _currentWindow = w as GraphWindow;
                w.DrawOneInstance();
            }

            _currentWindow = null;
        }

        private void FitViewToSelection()
        {
            var selection = SelectionManager.GetSelectedSymbolChildUis().ToArray();

            if (selection.Length == 0)
                return;

            var area = new ImRect(selection[0].PosOnCanvas,
                                  selection[0].PosOnCanvas + selection[0].Size);

            for (var index = 1; index < selection.Length; index++)
            {
                var selectedItem = selection[index];
                area.Add(new ImRect(selectedItem.PosOnCanvas,
                                    selectedItem.PosOnCanvas + selectedItem.Size));
            }

            area.Expand(400);

            _graphCanvas.FitAreaOnCanvas(area);
        }

        public static void SetBackgroundOutput(Instance instance)
        {
            if (_currentWindow == null)
                return;

            _currentWindow._imageBackground.BackgroundNodePath = instance != null
                                                                     ? NodeOperations.BuildIdPathForInstance(instance)
                                                                     : null;
        }

        protected override void DrawContent()
        {
            if (SelectionManager.FitViewToSelectionRequested)
                FitViewToSelection();
            
            _imageBackground.Draw();
            
            ImGui.SetCursorPos(Vector2.Zero);
            THelpers.DebugContentRect("window");
            {
                var drawList = ImGui.GetWindowDrawList();
                var contentHeight =0; 
                
                if (!UserSettings.Config.HideUiElementsInGraphWindow)
                {
                    var currentTimelineHeight = UsingCustomTimelineHeight ? _customTimeLineHeight : ComputedTimelineHeight;
                     if (CustomComponents.SplitFromBottom(ref currentTimelineHeight))
                     {
                          _customTimeLineHeight = (int)currentTimelineHeight;
                     }
                    
                    contentHeight = (int)ImGui.GetWindowHeight() - (int)currentTimelineHeight - 4; // Hack that also depends on when a window-title is being rendered
                }

                ImGui.BeginChild("##graph", new Vector2(0, contentHeight), false,
                                 ImGuiWindowFlags.NoScrollbar 
                                 | ImGuiWindowFlags.NoMove 
                                 | ImGuiWindowFlags.NoScrollWithMouse 
                                 | ImGuiWindowFlags.NoDecoration 
                                 | ImGuiWindowFlags.NoTitleBar
                                 | ImGuiWindowFlags.ChildWindow);
                {
                    drawList.ChannelsSplit(2);
                    drawList.ChannelsSetCurrent(1);
                    {
                        if (!UserSettings.Config.HideUiElementsInGraphWindow)
                        {
                            _graphCanvas.MakeCurrent();
                            TitleAndBreadCrumbs.Draw(_graphCanvas.CompositionOp);    
                        }
                        DrawControlsAtBottom();
                    }
                    
                    drawList.ChannelsSetCurrent(0);
                    {
                        _graphCanvas.Draw(drawList, showGrid: !_imageBackground.IsActive);
                    }
                    drawList.ChannelsMerge();
                }
                ImGui.EndChild();
                
                if (!UserSettings.Config.HideUiElementsInGraphWindow)
                {
                    var availableRestHeight = ImGui.GetContentRegionAvail().Y;
                    if (availableRestHeight <= 3)
                    {
                        Log.Warning($"skipping rending of timeline because layout is inconsistent: only {availableRestHeight}px left.");
                    }
                    else
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
                        ImGui.BeginChild("##timeline", Vector2.Zero, false,
                                         ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
                        {
                            _timeLineCanvas.Draw(_graphCanvas.CompositionOp);
                        }
                        ImGui.EndChild();
                    }
                }
            }
        }

        private void DrawControlsAtBottom()
        {
            ImGui.SetCursorPos(
                               new Vector2(
                                           ImGui.GetWindowContentRegionMin().X,
                                           ImGui.GetWindowContentRegionMax().Y - TimeControls.ControlSize.Y));

            if (CustomComponents.IconButton(UsingCustomTimelineHeight ? Icon.ChevronUp : Icon.ChevronDown,
                                            "##TimelineToggle", TimeControls.ControlSize))
            {
                _customTimeLineHeight = UsingCustomTimelineHeight ? UseComputedHeight : 200;
            }

            ImGui.SameLine();

            TimeControls.DrawTimeControls(ref _playback, _timeLineCanvas);
            if (_imageBackground.IsActive)
            {
                _imageBackground.DrawResolutionSelector();
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    _currentWindow._imageBackground.BackgroundNodePath = null;
                }
            }
        }

        protected override void Close()
        {
            GraphWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphWindow(); // Must call constructor
        }

        private static class TitleAndBreadCrumbs
        {
            public static void Draw(Instance compositionOp)
            {
                DrawBreadcrumbs(compositionOp);
                DrawNameAndDescription(compositionOp);
            }

            public static void DrawBreadcrumbs(Instance compositionOp)
            {
                ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
                IEnumerable<Instance> parents = GraphCanvas.GetParents(compositionOp);

                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushFont(Fonts.FontSmall);
                {
                    foreach (var p in parents)
                    {
                        ImGui.SameLine();
                        ImGui.PushID(p.SymbolChildId.GetHashCode());

                        var clicked = ImGui.Button(p.Symbol.Name);

                        if (clicked)
                        {
                            GraphCanvas.Current.SetCompositionToParentInstance(p);
                            break;
                        }

                        ImGui.SameLine();
                        ImGui.PopID();
                        ImGui.Text(">");
                    }
                }
                ImGui.PopFont();
                ImGui.PopStyleColor();
            }

            public static void DrawNameAndDescription(Instance compositionOp)
            {
                ImGui.SetCursorPosX(8);
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.Text(compositionOp.Symbol.Name);
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.3f).Rgba);
                ImGui.Text("  - " + compositionOp.Symbol.Namespace);
                ImGui.PopFont();
                ImGui.PopStyleColor();

                var symbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];

                if (symbolUi.Description == null)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);

                    ImGui.PushFont(Fonts.FontSmall);
                    if (ImGui.Button("add description..."))
                    {
                        symbolUi.Description = " ";
                        _justAddedDescription = false;
                    }

                    ImGui.PopFont();
                    ImGui.PopStyleColor(2);
                }
                else
                {
                    if (symbolUi.Description == string.Empty)
                    {
                        symbolUi.Description = null;
                    }
                    else
                    {
                        var desc = symbolUi.Description;
                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                        {
                            var sizeMatchingDescription = ImGui.CalcTextSize(desc) + new Vector2(20, 40);
                            sizeMatchingDescription.X = Math.Max(300, sizeMatchingDescription.X);
                            if (_justAddedDescription)
                            {
                                ImGui.SetKeyboardFocusHere();
                                _justAddedDescription = false;
                            }

                            ImGui.InputTextMultiline("##description", ref desc, 3000, sizeMatchingDescription);
                        }
                        ImGui.PopStyleColor(2);
                        ImGui.PopFont();
                        symbolUi.Description = desc;
                    }
                }
            }

            private static bool _justAddedDescription;
        }

        
        private readonly ImageBackground _imageBackground = new ImageBackground();

        private readonly GraphCanvas _graphCanvas;
        private Playback _playback;
        private const int UseComputedHeight = -1;
        private int _customTimeLineHeight = UseComputedHeight;
        private bool UsingCustomTimelineHeight => _customTimeLineHeight > UseComputedHeight;

        private float ComputedTimelineHeight => (_timeLineCanvas.SelectedAnimationParameters.Count * DopeSheetArea.LayerHeight)
                                              + _timeLineCanvas.LayersArea.LastHeight
                                              + TimeLineCanvas.TimeLineDragHeight
                                              + 2;

        private readonly TimeLineCanvas _timeLineCanvas;
    }
}