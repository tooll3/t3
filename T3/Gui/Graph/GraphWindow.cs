using System;
using ImGuiNET;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
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

            const string trackName = @"Resources\lorn-sega-sunset.mp3";
            _clipTime = File.Exists(trackName) ? new StreamClipTime(trackName) : new ClipTime();

            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);
            var shownOpInstance = FindIdInNestedChildren(T3Ui.UiModel.RootInstance, opId) ?? T3Ui.UiModel.RootInstance;
            var path = BuildIdPathForInstance(shownOpInstance);
            GraphCanvas = new GraphCanvas(this, path);

            _timeLineCanvas = new TimeLineCanvas(_clipTime);

            WindowFlags = ImGuiWindowFlags.NoScrollbar;
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

        private static List<Guid> BuildIdPathForInstance(Instance instance)
        {
            return CollectAnIdPathForInstance(T3Ui.UiModel.RootInstance, new List<Guid>());

            List<Guid> CollectAnIdPathForInstance(Instance cursor, List<Guid> path)
            {
                if (cursor.SymbolChildId == instance.SymbolChildId)
                {
                    Debug.Assert(path.Count == 0);
                    path.Add(cursor.SymbolChildId); // found searched instance
                    return path;
                }

                foreach (var subChild in cursor.Children)
                {
                    var result = CollectAnIdPathForInstance(subChild, path);
                    if (result != null)
                    {
                        path.Insert(0, cursor.SymbolChildId);
                        return result;
                    }
                }

                return null;
            }
        }

        private static int _instanceCounter;
        private static readonly List<Window> GraphWindowInstances = new List<Window>();

        public override List<Window> GetInstances()
        {
            return GraphWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
            _clipTime.Update();
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in GraphWindowInstances)
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

                CustomComponents.SplitFromBottom(ref _heightTimeLine);
                var graphHeight = ImGui.GetWindowHeight() - _heightTimeLine - 30;

                ImGui.BeginChild("##graph", new Vector2(0, graphHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    dl.ChannelsSplit(2);
                    dl.ChannelsSetCurrent(1);
                    {
                        DrawBreadcrumbs();
                        DrawBreadcrumbsNameAndDescription();
                        TimeControls.DrawTimeControls(_clipTime, ref _timeLineCanvas.Mode);
                    }
                    dl.ChannelsSetCurrent(0);
                    {
                        GraphCanvas.Draw(dl);
                    }
                    dl.ChannelsMerge();
                }
                ImGui.EndChild();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
                ImGui.BeginChild("##timeline", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    DrawTimelineAndCurveEditor();
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

        private void DrawTimelineAndCurveEditor()
        {
            _timeLineCanvas.Draw(GraphCanvas.CompositionOp, GetCurvesForSelectedNodes());
        }

        public struct AnimationParameter
        {
            public IEnumerable<Curve> Curves;
            public IInputSlot Input;
            public Instance Instance;
        }

        private List<AnimationParameter> GetCurvesForSelectedNodes()
        {
            var selection = GraphCanvas.SelectionHandler.SelectedElements;
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
                                                 Curves = animator.GetCurvesForInput(input)
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

        private readonly ClipTime _clipTime;
        private static float _heightTimeLine = 200;
        private readonly TimeLineCanvas _timeLineCanvas;
    }
}