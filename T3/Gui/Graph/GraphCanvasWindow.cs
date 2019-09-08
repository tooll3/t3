using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Animation;
using UiHelpers;
using static ImGuiNET.ImGui;
using static T3.Gui.CustomComponents;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphCanvasWindow
    {
        public GraphCanvas Canvas { get; private set; }


        public GraphCanvasWindow(Instance opInstance, string windowTitle = "Graph windows")
        {
            _windowTitle = windowTitle;
            Canvas = new GraphCanvas(opInstance);
            _curveEditor = new CurveEditCanvas(_clipTime);
        }


        public bool Draw()
        {
            bool opened = true;
            _clipTime.Update();

            PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            if (Begin(_windowTitle, ref opened, ImGuiWindowFlags.NoScrollbar))
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
                        DrawTimeControls();
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

            End();
            return opened;
        }


        private void DrawTimeControls()
        {
            SetCursorPos(
                new Vector2(
                    GetWindowContentRegionMin().X,
                    GetWindowContentRegionMax().Y - 30));

            TimeSpan timespan = TimeSpan.FromSeconds(_clipTime.Time);

            var delta = 0.0;
            if (CustomComponents.JogDial(timespan.ToString(@"hh\:mm\:ss\:ff"), ref delta, new Vector2(80, 0)))
            {
                _clipTime.PlaybackSpeed = 0;
                _clipTime.Time += delta;
            }

            SameLine();
            Button("[<", _timeControlsSize);
            SameLine();
            Button("<<", _timeControlsSize);
            SameLine();

            var isPlayingBackwards = _clipTime.PlaybackSpeed < 0;
            if (CustomComponents.ToggleButton(
                    label: isPlayingBackwards ? $"[{(int)_clipTime.PlaybackSpeed}x]" : "<",
                    ref isPlayingBackwards,
                    _timeControlsSize))
            {
                if (_clipTime.PlaybackSpeed != 0)
                {
                    _clipTime.PlaybackSpeed = 0;
                }
                else if (_clipTime.PlaybackSpeed == 0)
                {
                    _clipTime.PlaybackSpeed = -1;
                }
            }
            SameLine();


            // Play forward
            var isPlaying = _clipTime.PlaybackSpeed > 0;
            if (CustomComponents.ToggleButton(
                    label: isPlaying ? $"[{(int)_clipTime.PlaybackSpeed}x]" : ">",
                    ref isPlaying,
                    _timeControlsSize,
                    trigger: KeyboardBinding.Triggered(UserAction.PlaybackToggle)))
            {
                if (_clipTime.PlaybackSpeed != 0)
                {
                    _clipTime.PlaybackSpeed = 0;
                }
                else if (_clipTime.PlaybackSpeed == 0)
                {
                    _clipTime.PlaybackSpeed = 1;
                }
            }

            if (KeyboardBinding.Triggered(UserAction.PlaybackBackwards))
            {
                if (_clipTime.PlaybackSpeed >= 0)
                {
                    _clipTime.PlaybackSpeed = -1;
                }
                else if (_clipTime.PlaybackSpeed > -8)
                {
                    _clipTime.PlaybackSpeed *= 2;
                }
            }

            if (KeyboardBinding.Triggered(UserAction.PlaybackForward))
            {
                if (_clipTime.PlaybackSpeed <= 0)
                {
                    _clipTime.PlaybackSpeed = 1;
                }
                else if (_clipTime.PlaybackSpeed < 8)
                {
                    _clipTime.PlaybackSpeed *= 2;
                }
            }

            if (KeyboardBinding.Triggered(UserAction.PlaybackStop))
            {
                _clipTime.PlaybackSpeed = 0;
            }

            SameLine();
            Button(">>", _timeControlsSize);
            SameLine();
            Button(">]", _timeControlsSize);
            SameLine();
            CustomComponents.ToggleButton("Loop", ref _clipTime.IsLooping, _timeControlsSize);
        }

        private void DrawTimelineAndCurveEditor()
        {
            SetCurvesForSelection();
            _curveEditor.Draw();
            //DrawDragTimeArea();
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
                                      where animator.AnimatedInputCurves.ContainsKey(input.Id)
                                      select animator.AnimatedInputCurves[input.Id]).ToList();

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

        private string _windowTitle;

        private ClipTime _clipTime = new ClipTime();
        private static float _heightTimeLine = 100;
        private CurveEditCanvas _curveEditor;

        // Styling properties
        public static Vector2 _timeControlsSize = new Vector2(40, 0);
    }
}