using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Selection;
using T3.Logging;

namespace T3.Gui.Graph
{
    public class GraphCanvas
    {
        public GraphCanvas(Instance opInstance)
        {
            CompositionOp = opInstance;
            _selectionFence = new SelectionFence(this);
        }

        public void Draw()
        {
            if (!SymbolChildUiRegistry.Entries.ContainsKey(CompositionOp.Symbol.Id))
            {
                SymbolChildUiRegistry.Entries[CompositionOp.Symbol.Id] = new Dictionary<Guid, SymbolChildUi>();
                Log.Debug("Added Op to UI Registry " + CompositionOp.Symbol.SymbolName);
            }

            UiChildrenById = SymbolChildUiRegistry.Entries[CompositionOp.Symbol.Id];
            DrawList = ImGui.GetWindowDrawList();
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
                    DrawList.PushClipRect(_canvasWindowPos, _canvasWindowPos + _size);

                    // Canvas interaction --------------------------------------------
                    if (ImGui.IsWindowHovered())
                    {
                        if (ImGui.IsMouseDragging(1))
                        {
                            _scrollTarget += _io.MouseDelta;
                        }

                        if (!ImGui.IsAnyItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), CompositionOp.Symbol, CanvasPosFromScreen(ImGui.GetMousePos()));
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
                    ConnectionLine.DrawAll(this);

                    if (ImGui.IsMouseReleased(0))
                    {
                        DraftConnection.Cancel();
                    }

                    _selectionFence.Draw();
                    DrawList.PopClipRect();
                    DrawContextMenu();
                }
                ImGui.EndChild();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);
            }
            ImGui.EndGroup();
        }


        private void DrawContextMenu()
        {
            // Open context menu
            if (!ImGui.IsAnyItemHovered() && ImGui.IsWindowHovered() && ImGui.IsMouseClicked(1))
            {
                _contextMenuOpened = true;
            }

            // Draw context menu
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                Vector2 scene_pos = ImGui.GetMousePosOnOpeningCurrentPopup();// - scrollOffset;


                // Todo: Convert to linc
                var selectedChildren = new List<SymbolChildUi>();
                foreach (var x in SelectionHandler.SelectedElements)
                {
                    var childUi = x as SymbolChildUi;
                    if (childUi != null)
                    {
                        selectedChildren.Add(childUi);
                    }
                }

                if (selectedChildren.Count > 0)
                {
                    var label = selectedChildren.Count == 1
                        ? $"{selectedChildren[0].ReadableName} Item..." : $"{selectedChildren.Count} Items...";

                    ImGui.Text(label);
                    if (ImGui.MenuItem(" Rename..", null, false, false)) { }
                    if (ImGui.MenuItem(" Delete", null))
                    {
                        Log.Warning("Not implemented yet");
                        foreach (var x in selectedChildren)
                        {
                            // TODO: Add implementation
                        }
                    }
                    if (ImGui.MenuItem(" Copy", null, false, false)) { }
                    ImGui.Separator();
                }
                if (ImGui.MenuItem("Rename..", null, false, false)) { }
                if (ImGui.MenuItem("Add")) { }
                if (ImGui.MenuItem("Paste", null, false, false)) { }
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();
        }


        private void DrawGrid()
        {
            var gridSize = 64.0f * _scale;
            for (float x = _scroll.X % gridSize; x < _size.X; x += gridSize)
            {
                DrawList.AddLine(
                    new Vector2(x, 0.0f) + _canvasWindowPos,
                    new Vector2(x, _size.Y) + _canvasWindowPos,
                    new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }

            for (float y = _scroll.Y % gridSize; y < _size.Y; y += gridSize)
            {
                DrawList.AddLine(
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
            DrawList.AddRect(ScreenPosFromCanvas(rectOnCanvas.Min), ScreenPosFromCanvas(rectOnCanvas.Max), color);
        }

        public void DrawRectFilled(ImRect rectOnCanvas, Color color)
        {
            DrawList.AddRectFilled(ScreenPosFromCanvas(rectOnCanvas.Min), ScreenPosFromCanvas(rectOnCanvas.Max), color);
        }

        private ImDrawListPtr _overlayDrawList;
        private Vector2 _size;
        private Vector2 _mouse;

        public ImDrawListPtr DrawList;
        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);

        public Vector2 _canvasWindowPos;    // Position of the canvas window-panel within Application window
        public float _scale = 1;            // The damped scale factor {read only}
        float _scaleTarget = 1;

        public const float GridSize = 20f;

        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();
        private SelectionFence _selectionFence;

        private ImGuiIOPtr _io;
        public Instance CompositionOp { get; set; }

        public Dictionary<Guid, SymbolChildUi> UiChildrenById { get; private set; }
    }
}
