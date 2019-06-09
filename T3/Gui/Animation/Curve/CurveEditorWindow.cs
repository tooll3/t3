using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Animation.Curve;
using T3.Gui.Graph;
using T3.Gui.Selection;

namespace T3.Gui.Animation
{
    /// <summary>
    /// A stub window to collect curve editing functionality during implementation.
    /// ToDo:
    /// [x] Generate a mock curve with some random keyframes
    /// [ ] Render Curve
    /// [ ] Render time-line ticks
    /// [ ] Zoom and pan timeline-range
    /// [ ] Render value area
    /// [ ] Mock random-keyframes
    /// [ ] Selection of keyframes
    /// [ ] Edit Keyframes-tangent editing
    /// [ ] Implement Curve-Edit-Box
    /// </summary>
    class CurveEditorWindow
    {
        public CurveEditorWindow()
        {
            InitiailizeMockCurve();
        }


        private void DrawCurve()
        {
            foreach (var keyPair in _mockCurve.GetPoints())
            {
                var t = keyPair.Key;
                var def = keyPair.Value;
                var p = new Vector2((float)t, (float)def.Value) + _canvasWindowPos;
                DrawList.AddRectFilled(p, p + new Vector2(10, 10), Color.White);
            }
        }


        public bool Draw(ref bool opened)
        {
            _io = ImGui.GetIO();
            if (ImGui.Begin("Curve Editor", ref opened))
            {
                ImGui.BeginGroup();
                {
                    _mouse = ImGui.GetMousePos();
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Color(60, 60, 70, 200).Rgba);

                    // Damp scaling
                    _scale = Im.Lerp(_scale, _scaleTarget, _io.DeltaTime * 20);
                    _scroll = Im.Lerp(_scroll, _scrollTarget, _io.DeltaTime * 20);

                    THelpers.DebugWindowRect("window");
                    ImGui.BeginChild("scrolling_region", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                    {
                        DrawList = ImGui.GetWindowDrawList();

                        THelpers.DebugWindowRect("window.scrollingRegion");
                        _canvasWindowPos = ImGui.GetWindowPos();
                        _size = ImGui.GetWindowSize();
                        DrawList.PushClipRect(_canvasWindowPos, _canvasWindowPos + _size);

                        // Canvas interaction --------------------------------------------
                        if (ImGui.IsWindowHovered())
                        {
                            if (ImGui.IsMouseDragging(1))
                            {
                                _scrollTarget += _io.MouseDelta;
                            }

                            // Zoom with mouse wheel
                            if (_io.MouseWheel != 0)
                            {
                                const float zoomSpeed = 1.2f;
                                var focusCenter = (_mouse - _scroll - _canvasWindowPos) / _scale;

                                _foreground.AddCircle(focusCenter + ImGui.GetWindowPos(), 10, Color.TRed);

                                if (_io.MouseWheel < 0.0f)
                                {
                                    for (float zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                                    {
                                        _scaleTarget = Im.Max(0.3f, _scaleTarget / zoomSpeed);
                                    }
                                }

                                if (_io.MouseWheel > 0.0f)
                                {
                                    for (float zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                                    {
                                        _scaleTarget = Im.Min(3.0f, _scaleTarget * zoomSpeed);
                                    }
                                }

                                Vector2 shift = _scrollTarget + (focusCenter * _scaleTarget);
                                _scrollTarget += _mouse - shift - _canvasWindowPos;
                            }

                            ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
                        }


                        ////_selectionFence.Draw();
                        DrawCurve();
                        DrawList.PopClipRect();
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar(2);
                }
                ImGui.EndGroup();
            }

            ImGui.End();
            return opened;
        }

        private void InitiailizeMockCurve()
        {
            _mockCurve = new Curve();
            for (int i = 0; i < 10; i++)
            {
                _mockCurve.AddOrUpdateV(i * 20, new VDefinition()
                {
                    Value = random.NextDouble() * 50,
                    InTangentAngle = 0.0,
                    OutTangentAngle = 0.0,
                });
            }
        }


        private Curve _mockCurve = new Curve();
        private Random random = new Random();


        private ImDrawListPtr _foreground;
        private Vector2 _size;
        private Vector2 _mouse;

        public static ImDrawListPtr DrawList;
        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);

        public Vector2 _canvasWindowPos;    // Position of the canvas window-panel within Application window
        public float _scale = 1;            // The damped scale factor {read only}
        float _scaleTarget = 1;

        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();
        private SelectionFence _selectionFence;

        private ImGuiIOPtr _io;

    }
}
