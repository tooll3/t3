using System;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
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

            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);
            var shownOpInstance = FindIdInNestedChildren(T3Ui.UiModel.RootInstance, opId) ?? T3Ui.UiModel.RootInstance;
            var path = NodeOperations.BuildIdPathForInstance(shownOpInstance);
            _graphCanvas = new GraphCanvas(this, path);

            _timeLineCanvas = new TimeLineCanvas(_playback);

            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            GraphWindowInstances.Add(this);
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

        private static bool _justAddedDescription;

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
            if(_currentWindow == null)
                return;
            
            _currentWindow._imageBackground.BackgroundNodePath = instance != null
                                                                 ? NodeOperations.BuildIdPathForInstance(instance)
                                                                 : null;
        }

        private readonly ImageBackground _imageBackground = new ImageBackground();

        protected override void DrawContent()
        {
            if (SelectionManager.FitViewToSelectionRequested)
                FitViewToSelection();

            _imageBackground.Draw();
            ImGui.SetCursorPos(Vector2.Zero);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            {
                var dl = ImGui.GetWindowDrawList();

                var animationParameters = GetAnimationParametersForSelectedNodes();

                var isTimelineCollapsed = _heightTimeLine <= TimeLineCanvas.TimeLineDragHeight;
                var timelineHeight = isTimelineCollapsed
                                         ? (animationParameters.Count * DopeSheetArea.LayerHeight)
                                           + _timeLineCanvas.LayersArea.LastHeight
                                           + TimeLineCanvas.TimeLineDragHeight
                                           + 2
                                         : _heightTimeLine;

                if (CustomComponents.SplitFromBottom(ref timelineHeight))
                {
                    _heightTimeLine = timelineHeight;
                }

                const float ImgGuiTitleHeight = 4; // Hack that also depends on when a window-title is being rendered 

                var graphHeight = ImGui.GetWindowHeight() - timelineHeight - ImgGuiTitleHeight;
                ImGui.BeginChild("##graph", new Vector2(0, graphHeight), false,
                                 ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
                {
                    dl.ChannelsSplit(2);
                    dl.ChannelsSetCurrent(1);
                    {
                        DrawBreadcrumbs();
                        DrawBreadcrumbsNameAndDescription();

                        ImGui.SetCursorPos(
                                           new Vector2(
                                                       ImGui.GetWindowContentRegionMin().X,
                                                       ImGui.GetWindowContentRegionMax().Y - TimeControls.ControlSize.Y));

                        if (CustomComponents.IconButton(isTimelineCollapsed ? Icon.ChevronUp : Icon.ChevronDown,
                                                        "##TimelineToggle", TimeControls.ControlSize))
                        {
                            _heightTimeLine = isTimelineCollapsed ? 200 : TimeLineCanvas.TimeLineDragHeight;
                        }

                        ImGui.SameLine();

                        TimeControls.DrawTimeControls(_playback, _timeLineCanvas);
                        if(_imageBackground.IsActive)
                            _imageBackground.DrawResolutionSelector();
                    }
                    dl.ChannelsSetCurrent(0);
                    {
                        _graphCanvas.Draw(dl, showGrid:!_imageBackground.IsActive);
                    }
                    dl.ChannelsMerge();
                }
                ImGui.EndChild();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
                ImGui.BeginChild("##timeline", Vector2.Zero, false,
                                 ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
                {
                    _timeLineCanvas.Draw(_graphCanvas.CompositionOp, animationParameters);
                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
        }

        private void DrawBreadcrumbsNameAndDescription()
        {
            ImGui.SetCursorPosX(8);
            ImGui.PushFont(Fonts.FontLarge);
            ImGui.Text(_graphCanvas.CompositionOp.Symbol.Name);
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.3f).Rgba);
            ImGui.Text("  - " + _graphCanvas.CompositionOp.Symbol.Namespace);
            ImGui.PopFont();
            ImGui.PopStyleColor();

            var symbolUi = SymbolUiRegistry.Entries[_graphCanvas.CompositionOp.Symbol.Id];

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
                        sizeMatchingDescription.X = Im.Max(300, sizeMatchingDescription.X);
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

        protected override void Close()
        {
            GraphWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphWindow(); // Must call constructor
        }

        public struct AnimationParameter
        {
            public IEnumerable<Curve> Curves;
            public IInputSlot Input;
            public Instance Instance;
            public SymbolChildUi ChildUi;
        }

        private List<AnimationParameter> GetAnimationParametersForSelectedNodes()
        {
            var selection = SelectionManager.GetSelectedNodes<ISelectableNode>();
            var symbolUi = SymbolUiRegistry.Entries[_graphCanvas.CompositionOp.Symbol.Id];
            var animator = symbolUi.Symbol.Animator;
            var curvesForSelection = (from child in _graphCanvas.CompositionOp.Children
                                      from selectedElement in selection
                                      where child.SymbolChildId == selectedElement.Id
                                      from input in child.Inputs
                                      where animator.IsInputSlotAnimated(input)
                                      select new AnimationParameter()
                                                 {
                                                     Instance = child,
                                                     Input = input,
                                                     Curves = animator.GetCurvesForInput(input),
                                                     ChildUi = symbolUi.ChildUis.Single(childUi => childUi.Id == selectedElement.Id)
                                                 }).ToList();
            return curvesForSelection;
        }

        private void DrawBreadcrumbs()
        {
            ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
            IEnumerable<Instance> parents = _graphCanvas.GetParents();

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
                        _graphCanvas.SetCompositionToParentInstance(p);
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

        private readonly GraphCanvas _graphCanvas;
        private readonly Playback _playback;
        private float _heightTimeLine = TimeLineCanvas.TimeLineDragHeight;
        private readonly TimeLineCanvas _timeLineCanvas;
    }
}