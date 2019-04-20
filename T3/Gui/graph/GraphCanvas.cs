using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    public class GraphCanvas
    {
        public GraphCanvas(Instance opInstance)
        {
            _compositionOp = opInstance;
            _selectionFence = new SelectionFence(this);
        }

        public void Draw()
        {
            _uiChildren = SymbolChildUiRegistry.Entries[_compositionOp.Symbol.Id];
            _drawList = ImGui.GetWindowDrawList();
            _overlayDrawList = ImGui.GetOverlayDrawList();
            _io = ImGui.GetIO();

            ImGui.BeginGroup();
            {
                //ImGui.Text(_debugMessages); _debugMessages = "";

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
                    THelpers.DebugWindowRect("window.scrollingRegion");
                    _canvasWindowPos = ImGui.GetWindowPos();
                    _size = ImGui.GetWindowSize();
                    _drawList.PushClipRect(_canvasWindowPos, _canvasWindowPos + _size);

                    // Canvas interaction --------------
                    if (ImGui.IsWindowHovered())
                    {
                        if (ImGui.IsMouseDragging(1))
                        {
                            _scrollTarget += _io.MouseDelta;
                        }

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), _compositionOp.Symbol, CanvasPosFromScreen(ImGui.GetMousePos()));
                        }

                        // Zoom with mouse wheel
                        if (_io.MouseWheel != 0)
                        {
                            const float zoomSpeed = 1.2f;
                            var focusCenter = (_mouse - _scroll - _canvasWindowPos) / _scale;

                            _overlayDrawList.AddCircle(focusCenter + ImGui.GetWindowPos(), 10, Color.TRed);

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

                    // Draw Grid ------------
                    {
                        var gridSize = 64.0f * _scale;
                        for (float x = _scroll.X % gridSize; x < _size.X; x += gridSize)
                        {
                            _drawList.AddLine(
                                new Vector2(x, 0.0f) + _canvasWindowPos,
                                new Vector2(x, _size.Y) + _canvasWindowPos,
                                new Color(0.5f, 0.5f, 0.5f, 0.1f));
                        }

                        for (float y = _scroll.Y % gridSize; y < _size.Y; y += gridSize)
                        {
                            _drawList.AddLine(
                                new Vector2(0.0f, y) + _canvasWindowPos,
                                new Vector2(_size.X, y) + _canvasWindowPos,
                                new Color(0.5f, 0.5f, 0.5f, 0.1f));
                        }
                    }


                    // Draw nodes
                    foreach (var symbolChildUi in SymbolChildUiRegistry.Entries[_compositionOp.Symbol.Id].Values)
                    {
                        GraphOperator.DrawOnCanvas(symbolChildUi, this);
                    }

                    // Draw links
                    DrawConnections();

                    _selectionFence.Draw();

                    _drawList.PopClipRect();


                }
                ImGui.EndChild();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);


            }
            ImGui.EndGroup();
        }


        private void DrawConnections()
        {
            foreach (var c in _compositionOp.Symbol.Connections)
            {
                var source = UiChildrenById[c.SourceChildId];
                var target = UiChildrenById[c.TargetChildId];
                var sourcePos = ScreenPosFromCanvas(source.Position);
                var targetPos = ScreenPosFromCanvas(target.Position + new Vector2(0, target.Size.Y));


                _drawList.AddBezierCurve(
                    sourcePos,
                    sourcePos + new Vector2(0, -50),
                    targetPos + new Vector2(0, 50),
                    targetPos,
                    Color.White, 3f);

                _drawList.AddTriangleFilled(
                    targetPos + new Vector2(0, -3),
                    targetPos + new Vector2(4, 2),
                    targetPos + new Vector2(-4, 2),
                    Color.White);
            }
            //foreach (var link in _links)
            //{
            //    var node_inp = _nodes[link.OutputNodeIndex];
            //    var node_out = _nodes[link.InputNodeIndex];
            //    var p1 = GetScreenPosFrom(node_inp.GetOutputSlotPos(link.OutputSlotIndex));
            //    var p2 = GetScreenPosFrom(node_out.GetInputSlotPos(link.InputSlotIndex));
            //    _drawList.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, TColors.ToUint(200, 200, 100, 255), 3.0f);
            //}

            //// Draw Link under construction
            //if (_linkUnderConstruction != null)
            //{
            //    if (ImGui.IsMouseReleased(0))
            //    {
            //        //_linkUnderConstruction = null;
            //        return;
            //    }
            //    var luc = _linkUnderConstruction;
            //    var node_inp = luc.OutputNodeIndex == -1 ? null : _nodes[luc.OutputNodeIndex];
            //    var node_out = luc.InputNodeIndex == -1 ? null : _nodes[luc.InputNodeIndex];

            //    var p1 = node_inp == null ? ImGui.GetMousePos() : GetScreenPosFrom(node_inp.GetOutputSlotPos(luc.OutputSlotIndex));
            //    var p2 = node_out == null ? ImGui.GetMousePos() : GetScreenPosFrom(node_out.GetInputSlotPos(luc.InputSlotIndex));
            //    _drawList.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, TColors.ToUint(200, 200, 100, 255), 3.0f);
            //}
        }

        public void MoveSelected(Vector2 delta)
        {
            //foreach (var node in _nodes)
            //{
            //    if (!node.IsSelected)
            //        continue;

            //    node.Pos += delta;
            //}
        }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 CanvasPosFromScreen(Vector2 screenPos)
        {
            return (screenPos - _scroll - _canvasWindowPos) / _scale;
        }


        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ScreenPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll + _canvasWindowPos;
        }

        public ImRect CanvasRectFromScreen(ImRect screenRect)
        {
            return new ImRect(CanvasPosFromScreen(screenRect.Min), CanvasPosFromScreen(screenRect.Max));
        }

        public ImRect ScreenRectFromCanvas(ImRect canvasRect)
        {
            return new ImRect(ScreenPosFromCanvas(canvasRect.Min), ScreenPosFromCanvas(canvasRect.Max));
        }

        public void DrawRect(ImRect rectOnCanvas, Color color)
        {
            _drawList.AddRect(ScreenPosFromCanvas(rectOnCanvas.Min), ScreenPosFromCanvas(rectOnCanvas.Max), color);
        }

        public void DrawRectFilled(ImRect rectOnCanvas, Color color)
        {
            _drawList.AddRectFilled(ScreenPosFromCanvas(rectOnCanvas.Min), ScreenPosFromCanvas(rectOnCanvas.Max), color);
        }


        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll;
        }

        ImDrawListPtr _overlayDrawList;
        Vector2 _size;
        Vector2 _mouse;

        public ImDrawListPtr _drawList;
        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);



        public Vector2 _canvasWindowPos;    // Position of the canvas window-panel within Application window
        public float _scale = 1;            //The damped scale factor {read only}
        float _scaleTarget = 1;

        public const float GridSize = 20f;

        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();
        SelectionFence _selectionFence;

        ImGuiIOPtr _io;
        private Instance _compositionOp;

        public Dictionary<Guid, SymbolChildUi> UiChildrenById => _uiChildren;
        private Dictionary<Guid, SymbolChildUi> _uiChildren;
    }
}
