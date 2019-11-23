using System;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Windows;
using T3.Gui.Windows.TimeLine;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphWindow : Window
    {
        public GraphCanvas GraphCanvas { get; private set; }


        public GraphWindow() : base()
        {
            _instanceCounter++;
            Config.Title = "Graph##" + _instanceCounter;
            Config.Visible = true;
            AllowMultipleInstances = true;

            const string trackName = @"Resources\lorn-sega-sunset.mp3";
            _clipTime = File.Exists(trackName) ? new StreamClipTime(trackName) : new ClipTime();

            var opInstance = T3Ui.UiModel.MainOp;
            GraphCanvas = new GraphCanvas(opInstance);
            _timeLineCanvas = new TimeLineCanvas(_clipTime);

            WindowFlags = ImGuiWindowFlags.NoScrollbar;
            //WindowInstances.Add(this);
            _graphWindowInstances.Add(this);
        }

        private static int _instanceCounter = 0;
        private static readonly List<Window> _graphWindowInstances = new List<Window>();

        public override List<Window> GetInstances()
        {
            return _graphWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
            _clipTime.Update();
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in _graphWindowInstances)
            {
                w.DrawOneInstance();
            }
        }

        protected override void DrawContent()
        {
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
                        TimeControls.DrawTimeControls(_clipTime, ref _timeLineCanvas.Mode);
                    }
                    dl.ChannelsSetCurrent(0);
                    GraphCanvas.Draw(dl);
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

        protected override void Close()
        {
            _graphWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            new GraphWindow();
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
                                      where child.Id == selectedElement.Id
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

            foreach (var p in parents)
            {
                ImGui.PushID(p.Id.GetHashCode());
                if (ImGui.Button(p.Symbol.Name))
                {
                    GraphCanvas.OpenComposition(p, zoomIn: false);
                    break;
                }

                ImGui.SameLine();
                ImGui.PopID();
                ImGui.Text("/");
                ImGui.SameLine();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, Color.White.Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Black.Rgba);
            ImGui.Button(GraphCanvas.CompositionOp.Symbol.Name);
            ImGui.PopStyleColor(2);
        }

        //private TimeLineCanvas.TimelineModes _timelineMode = TimeLineCanvas.TimelineModes.Undefined;

        private readonly ClipTime _clipTime;
        private static float _heightTimeLine = 100;
        private readonly TimeLineCanvas _timeLineCanvas;
    }
}