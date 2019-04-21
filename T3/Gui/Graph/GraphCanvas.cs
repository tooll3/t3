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
            UiChildrenById = SymbolChildUiRegistry.Entries[_compositionOp.Symbol.Id];
            _drawList = ImGui.GetWindowDrawList();
            _overlayDrawList = ImGui.GetOverlayDrawList();
            _io = ImGui.GetIO();

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

                    DrawGrid();
                    DrawNodes();
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

        private void DrawGrid()
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


        private void DrawNodes()
        {
            foreach (var symbolChildUi in UiChildrenById.Values)
            {
                GraphOperator.DrawOnCanvas(symbolChildUi, this);
            }
        }


        #region Connections ======================================================================
        //public static class DraftConnection
        //{
        //    public static Symbol.Connection NewConnection = null;
        //    //public Symbol.Connection _draftConnectionType = null;
        //    private static SymbolChildUi _draftConnectionSource = null;
        //    private static int _draftConnectionIndex = 0;
        //    private static Type _draftConnectionType = null;

        //    public static bool IsInputMatchingDraftConnection(Symbol.InputDefinition inputDef)
        //    {
        //        return inputDef.DefaultValue.ValueType == _draftConnectionType;
        //    }

        //    public static bool IsOutputMatchingDraftConnection(Symbol.InputDefinition outputDef)
        //    {
        //        return outputDef.DefaultValue.ValueType == _draftConnectionType;
        //    }

        //    public static bool IsDraftConnectionSource(SymbolChildUi childUi, int outputIndex)
        //    {
        //        return _draftConnectionSource == childUi && _draftConnectionIndex == outputIndex;
        //    }


        //    public static void StartNewConnection(Symbol.Connection newConnection)
        //    {
        //        NewConnection = newConnection;
        //    }

        //    public static void StartConnectionFromOutput(SymbolChildUi ui, int outputIndex)
        //    {
        //        NewConnection = new Symbol.Connection(
        //            sourceChildId: ui.SymbolChild.Id,
        //            outputDefinitionId: ui.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id,
        //            targetChildId: Guid.Empty,
        //            inputDefinitionId: Guid.Empty
        //        );
        //        _draftConnectionSource = ui;

        //    }

        //    public static void UpdateNewConnection()
        //    {

        //    }

        //    public static void CompleteNewConnection()
        //    {
        //        NewConnection = null;
        //    }
        //}


        private void DrawConnections()
        {
            foreach (var c in _compositionOp.Symbol.Connections)
            {
                DrawConnection(c);
            }

            if (DraftConnection.TempConnection != null)
                DrawConnection(DraftConnection.TempConnection);

        }

        private void DrawConnection(Symbol.Connection c)
        {
            Vector2 sourcePos;
            if (c.SourceChildId == Guid.Empty)
            {
                sourcePos = ImGui.GetMousePos();
            }
            else
            {
                var source = UiChildrenById[c.SourceChildId];
                sourcePos = ScreenPosFromCanvas(source.Position);
            }

            Vector2 targetPos;
            if (c.TargetChildId == Guid.Empty)
            {
                targetPos = ImGui.GetMousePos();
            }
            else
            {
                var target = UiChildrenById[c.TargetChildId];
                targetPos = ScreenPosFromCanvas(target.Position + new Vector2(0, target.Size.Y));
            }

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
        #endregion


        #region canvas scaling conversion =================================================================
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

        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll;
        }
        #endregion


        public void DrawRect(ImRect rectOnCanvas, Color color)
        {
            _drawList.AddRect(ScreenPosFromCanvas(rectOnCanvas.Min), ScreenPosFromCanvas(rectOnCanvas.Max), color);
        }

        public void DrawRectFilled(ImRect rectOnCanvas, Color color)
        {
            _drawList.AddRectFilled(ScreenPosFromCanvas(rectOnCanvas.Min), ScreenPosFromCanvas(rectOnCanvas.Max), color);
        }

        private ImDrawListPtr _overlayDrawList;
        private Vector2 _size;
        private Vector2 _mouse;

        public ImDrawListPtr _drawList;
        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);

        public Vector2 _canvasWindowPos;    // Position of the canvas window-panel within Application window
        public float _scale = 1;            // The damped scale factor {read only}
        float _scaleTarget = 1;

        public const float GridSize = 20f;

        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();
        private SelectionFence _selectionFence;

        private ImGuiIOPtr _io;
        private Instance _compositionOp;

        public Dictionary<Guid, SymbolChildUi> UiChildrenById { get; private set; }
    }
}
