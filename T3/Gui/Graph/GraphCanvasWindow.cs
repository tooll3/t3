using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Animation;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphCanvasWindow
    {
        public GraphCanvasWindow(Instance opInstance, string windowTitle = "Graph windows")
        {
            _windowTitle = windowTitle;
            Canvas = new GraphCanvas(opInstance);
            _curveEditor.ClipTime = _clipTime;
        }

        private ClipTime _clipTime = new ClipTime();

        private float GetGraphHeight()
        {
            return ImGui.GetWindowHeight() - _heightTimeLine - 30;
        }


        public bool Draw()
        {
            bool opened = true;
            _clipTime.Update();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            if (ImGui.Begin(_windowTitle, ref opened, ImGuiWindowFlags.NoScrollbar))
            {
                var dl = ImGui.GetWindowDrawList();
                //dl.ChannelsSplit(2);
                //dl.ChannelsSetCurrent(0);

                SplitFromBottom(ref _heightTimeLine);
                //dl.ChannelsSetCurrent(1);
                ImGui.BeginChild("##graph", new Vector2(0, GetGraphHeight()), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    //Im.DrawContentRegion();                    
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
                ImGui.EndChild();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
                ImGui.BeginChild("##timeline", Vector2.Zero, false, ImGuiWindowFlags.NoMove);
                {
                    DrawTimeline();
                }
                ImGui.EndChild();
                //dl.ChannelsMerge();

            }
            ImGui.PopStyleVar();

            ImGui.End();
            return opened;
        }


        public static Vector2 _timeControlsSize = new Vector2(40, 0);
        private void DrawTimeControls()
        {
            ImGui.SetCursorPos(
                new Vector2(
                    ImGui.GetWindowContentRegionMin().X,
                    ImGui.GetWindowContentRegionMax().Y - 30));

            TimeSpan timespan = TimeSpan.FromSeconds(_clipTime.Time);

            var delta = 0.0;
            if (JogDial(timespan.ToString(@"hh\:mm\:ss\:ff"), ref delta, new Vector2(80, 0)))
            {
                _clipTime.PlaybackSpeed = 0;
                _clipTime.Time += delta;
            }

            ImGui.SameLine();
            ImGui.Button("[<", _timeControlsSize);
            ImGui.SameLine();
            ImGui.Button("<<", _timeControlsSize);
            ImGui.SameLine();

            var isPlayingBackwards = _clipTime.PlaybackSpeed < 0;
            if (ToggleButton(
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
            ImGui.SameLine();


            // Play forward
            var isPlaying = _clipTime.PlaybackSpeed > 0;
            if (ToggleButton(
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

            ImGui.SameLine();
            ImGui.Button(">>", _timeControlsSize);
            ImGui.SameLine();
            ImGui.Button(">]", _timeControlsSize);
            ImGui.SameLine();
            ToggleButton("Loop", ref _clipTime.IsLooping, _timeControlsSize);
        }


        private void DrawTimeline()
        {
            SetCurvesForSelection();
            _curveEditor.Draw();
        }

        private void SetCurvesForSelection()
        {
            var selection = Canvas.SelectionHandler.SelectedElements;
            var symbolUi = SymbolUiRegistry.Entries[Canvas.CompositionOp.Symbol.Id];
            var animator = symbolUi.Animator;
            var curvesForSelection = (from child in Canvas.CompositionOp.Children
                                      from selectedElement in selection
                                      where child.Id == selectedElement.Id
                                      from input in child.Inputs
                                      where animator.AnimatedInputCurves.ContainsKey(input.Id)
                                      select animator.AnimatedInputCurves[input.Id]).ToList();

            _curveEditor.SetCurves(curvesForSelection);
        }

        public static bool JogDial(string label, ref double delta, Vector2 size)
        {
            var hot = ImGui.Button(label + "###dummy", size);
            var io = ImGui.GetIO();
            if (ImGui.IsItemActive())
            {
                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.GetForegroundDrawList().AddCircle(center, 100, Color.Gray, 50);
                hot = true;

                var pLast = io.MousePos - io.MouseDelta - center;
                var pNow = io.MousePos - center;
                var aLast = Math.Atan2(pLast.X, pLast.Y);
                var aNow = Math.Atan2(pNow.X, pNow.Y);
                delta = aNow - aLast;
                if (delta > 1.5)
                {
                    delta -= 2 * Math.PI;
                }
                else if (delta < -1.5)
                {
                    delta += 2 * Math.PI;
                }
            }
            return hot;
        }


        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>

        private static float _heightTimeLine = 100;
        private CurveEditCanvas _curveEditor = new CurveEditCanvas();


        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        void SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 5; ;

            var backup_pos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 1));

            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                offsetFromBottom = Im.Clamp(
                    offsetFromBottom - ImGui.GetIO().MouseDelta.Y,
                    0,
                    size.Y - thickness);
            }

            ImGui.SetCursorPos(backup_pos);
        }


        private bool ToggleButton(string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);
            }
            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }
            return clicked;
        }



        private void DrawBreadcrumbs()
        {
            ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
            List<Instance> parents = Canvas.GetParents();

            foreach (var p in parents)
            {
                ImGui.PushID(p.Id.GetHashCode());
                if (ImGui.Button(p.Symbol.Name))
                {
                    Canvas.CompositionOp = p;
                }

                ImGui.SameLine();
                ImGui.PopID();
                ImGui.Text("/");
                ImGui.SameLine();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, Color.White.Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Black.Rgba);
            ImGui.Button(Canvas.CompositionOp.Symbol.Name);
            ImGui.PopStyleColor(2);
        }

        private string _windowTitle;
        public GraphCanvas Canvas { get; private set; }
    }
}