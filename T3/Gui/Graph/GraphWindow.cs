using System;
using ImGuiNET;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphWindow : Window
    {
        public GraphCanvas GraphCanvas { get; private set; }

        public GraphWindow()
        {
            PreventWindowDragging = false; // conflicts with splitter between graph and timeline
            _instanceCounter++;
            Config.Title = "Graph##" + _instanceCounter;
            Config.Visible = true;
            AllowMultipleInstances = true;

            const string trackName = @"Resources\soundtrack\lorn-sega-sunset.mp3";
            _playback = File.Exists(trackName) ? new StreamPlayback(trackName) : new Playback();

            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);
            var shownOpInstance = FindIdInNestedChildren(T3Ui.UiModel.RootInstance, opId) ?? T3Ui.UiModel.RootInstance;
            var path = NodeOperations.BuildIdPathForInstance(shownOpInstance);
            GraphCanvas = new GraphCanvas(this, path);

            _timeLineCanvas = new TimeLineCanvas(_playback);

            WindowFlags = ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoScrollWithMouse;
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
            _playback.Update(ImGui.GetIO().DeltaTime);
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in GraphWindowInstances.ToArray())
            {
                w.DrawOneInstance();
            }
        }

        private static bool _justAddedDescription;

        /// <summary>
        /// References to composition op might be outdated because
        /// the symbol has been recompiled or otherwise changed.
        /// This uses the references to 
        /// </summary>
        private void UpdateReferences()
        {
        }

        protected override void DrawContent()
        {
            UpdateReferences();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            {
                var dl = ImGui.GetWindowDrawList();

                var animationParameters = GetAnimationParametersForSelectedNodes();
                
                var isTimelineCollapsed = _heightTimeLine <= TimeLineCanvas.TimeLineDragHeight;
                var timelineHeight = isTimelineCollapsed
                                         ? (animationParameters.Count * DopeSheetArea.LayerHeight)+ TimeLineCanvas.TimeLineDragHeight
                                         : _heightTimeLine;

                if (CustomComponents.SplitFromBottom(ref timelineHeight))
                {
                    _heightTimeLine = timelineHeight;
                }

                var graphHeight = ImGui.GetWindowHeight() - timelineHeight - 30;
                ImGui.BeginChild("##graph", new Vector2(0, graphHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
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

                        TimeControls.DrawTimeControls(_playback, ref _timeLineCanvas.Mode);
                    }
                    dl.ChannelsSetCurrent(0);
                    {
                        GraphCanvas.Draw(dl);
                    }
                    dl.ChannelsMerge();
                }
                ImGui.EndChild();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);
                ImGui.BeginChild("##timeline", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
                {
                    _timeLineCanvas.Draw(GraphCanvas.CompositionOp, animationParameters);
                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
        }

        private void DrawBreadcrumbsNameAndDescription()
        {
            ImGui.SetCursorPosX(8);
            ImGui.PushFont(Fonts.FontLarge);
            ImGui.Text(GraphCanvas.CompositionOp.Symbol.Name);
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.3f).Rgba);
            ImGui.Text("  - " + GraphCanvas.CompositionOp.Symbol.Namespace);
            ImGui.PopFont();
            ImGui.PopStyleColor();

            var symbolUi = SymbolUiRegistry.Entries[GraphCanvas.CompositionOp.Symbol.Id];

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
            var symbolUi = SymbolUiRegistry.Entries[GraphCanvas.CompositionOp.Symbol.Id];
            var animator = symbolUi.Symbol.Animator;
            var curvesForSelection = (from child in GraphCanvas.CompositionOp.Children
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
            IEnumerable<Instance> parents = GraphCanvas.GetParents();

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
                        GraphCanvas.SetCompositionToParentInstance(p);
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

        private readonly Playback _playback;
        private float _heightTimeLine = TimeLineCanvas.TimeLineDragHeight;
        private readonly TimeLineCanvas _timeLineCanvas;
    }
}