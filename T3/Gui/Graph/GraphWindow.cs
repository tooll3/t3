using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Windows;
using UiHelpers;
using static ImGuiNET.ImGui;
using static T3.Gui.CustomComponents;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphWindow : Window
    {
        public GraphCanvas Canvas { get; private set; }

        public static List<GraphWindow> WindowInstances = new List<GraphWindow>();

        public GraphWindow() : base()
        {
            _instanceCounter++;
            _title = "Graph##" + _instanceCounter;
            _visible = true;
            _allowMultipeInstances = true;

            var opInstance = T3UI.UiModel.MainOp;
            Canvas = new GraphCanvas(opInstance);
            _curveEditor = new CurveEditCanvas(_clipTime);

            _windowFlags = ImGuiWindowFlags.NoScrollbar;
            WindowInstances.Add(this);
        }

        static int _instanceCounter = 0;

        protected override void UpdateBeforeDraw()
        {
            _clipTime.Update();
        }


        protected override void DrawAllInstances()
        {
            foreach (var w in new List<GraphWindow>(WindowInstances))
            {
                w.DrawOneInstance();
            }
        }


        protected override void DrawContent()
        {
            PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            {
                var dl = GetWindowDrawList();

                SplitFromBottom(ref _heightTimeLine);
                var graphHeight = GetWindowHeight() - _heightTimeLine - 30;

                BeginChild("##graph", new Vector2(0, graphHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    dl.ChannelsSplit(2);
                    dl.ChannelsSetCurrent(1);
                    {
                        DrawBreadcrumbs();
                        TimeControls.DrawTimeControls(_clipTime, _curveEditor);
                    }
                    dl.ChannelsSetCurrent(0);
                    Canvas.Draw();
                    dl.ChannelsMerge();
                }
                EndChild();
                SetCursorPosY(GetCursorPosY() + 4);
                BeginChild("##timeline", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    DrawTimelineAndCurveEditor();
                }
                EndChild();
            }
            PopStyleVar();
        }


        protected override void Close()
        {
            WindowInstances.Remove(this);
        }


        protected override void AddAnotherInstance()
        {
            new GraphWindow();
        }



        private void DrawTimelineAndCurveEditor()
        {
            SetCurvesForSelection();
            _curveEditor.Draw();
        }


        private void SetCurvesForSelection()
        {
            var selection = Canvas.SelectionHandler.SelectedElements;
            var symbolUi = SymbolUiRegistry.Entries[Canvas.CompositionOp.Symbol.Id];
            var animator = symbolUi.Symbol.Animator;
            var curvesForSelection = (from child in Canvas.CompositionOp.Children
                                      from selectedElement in selection
                                      where child.Id == selectedElement.Id
                                      from input in child.Inputs
                                      where animator.IsInputSlotAnimated(input)
                                      select animator.GetCurvesForInput(input)).SelectMany(d => d).ToList();

            _curveEditor.SetCurves(curvesForSelection);
        }


        private void DrawBreadcrumbs()
        {
            SetCursorScreenPos(GetWindowPos() + new Vector2(1, 1));
            List<Instance> parents = Canvas.GetParents();

            foreach (var p in parents)
            {
                PushID(p.Id.GetHashCode());
                if (Button(p.Symbol.Name))
                {
                    Canvas.CompositionOp = p;
                }

                SameLine();
                PopID();
                Text("/");
                SameLine();
            }

            PushStyleColor(ImGuiCol.Button, Color.White.Rgba);
            PushStyleColor(ImGuiCol.Text, Color.Black.Rgba);
            Button(Canvas.CompositionOp.Symbol.Name);
            PopStyleColor(2);
        }


        private ClipTime _clipTime = new ClipTime();
        private static float _heightTimeLine = 100;
        private CurveEditCanvas _curveEditor;
    }
}