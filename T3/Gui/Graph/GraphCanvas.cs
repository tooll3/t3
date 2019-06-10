using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ICanvas
    {
        public GraphCanvas(Instance opInstance)
        {
            CompositionOp = opInstance;
            _selectionFence = new SelectionFence(this);
        }

        #region drawing UI ====================================================================
        public void Draw()
        {
            Current = this;
            if (!SymbolChildUiRegistry.Entries.ContainsKey(CompositionOp.Symbol.Id))
            {
                SymbolChildUiRegistry.Entries[CompositionOp.Symbol.Id] = new Dictionary<Guid, SymbolChildUi>();
                Log.Debug("Added Op to UI Registry " + CompositionOp.Symbol.SymbolName);
            }

            UiChildrenById = SymbolChildUiRegistry.Entries[CompositionOp.Symbol.Id];
            DrawList = ImGui.GetWindowDrawList();
            _foreground = ImGui.GetForegroundDrawList();
            _io = ImGui.GetIO();

            ImGui.BeginGroup();
            {
                _mouse = ImGui.GetMousePos();
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Color(60, 60, 70, 200).Rgba);

                // Damp scaling
                Scale = Im.Lerp(Scale, _scaleTarget, _io.DeltaTime * 20);
                _scroll = Im.Lerp(_scroll, _scrollTarget, _io.DeltaTime * 20);

                THelpers.DebugWindowRect("window");
                ImGui.BeginChild("scrolling_region", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    THelpers.DebugWindowRect("window.scrollingRegion");
                    WindowPos = ImGui.GetWindowPos();
                    _size = ImGui.GetWindowSize();
                    DrawList.PushClipRect(WindowPos, WindowPos + _size);

                    // Canvas interaction --------------------------------------------
                    if (ImGui.IsWindowHovered())
                    {
                        if (ImGui.IsMouseDragging(1))
                        {
                            _scrollTarget += _io.MouseDelta;
                        }

                        if (!ImGui.IsAnyItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), CompositionOp.Symbol, InverseTransformPosition(ImGui.GetMousePos()));
                        }

                        // Zoom with mouse wheel
                        if (_io.MouseWheel != 0)
                        {
                            const float zoomSpeed = 1.2f;
                            var focusCenter = (_mouse - _scroll - WindowPos) / Scale;

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
                            _scrollTarget += _mouse - shift - WindowPos;
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
            var gridSize = 64.0f * Scale;
            for (float x = _scroll.X % gridSize; x < _size.X; x += gridSize)
            {
                DrawList.AddLine(
                    new Vector2(x, 0.0f) + WindowPos,
                    new Vector2(x, _size.Y) + WindowPos,
                    new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }

            for (float y = _scroll.Y % gridSize; y < _size.Y; y += gridSize)
            {
                DrawList.AddLine(
                    new Vector2(0.0f, y) + WindowPos,
                    new Vector2(_size.X, y) + WindowPos,
                    new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }
        }


        private void DrawNodes()
        {
            foreach (var symbolChildUi in UiChildrenById.Values)
            {
                GraphOperator.Draw(symbolChildUi);
            }

            InputNodes.DrawAll();
            OutputNodes.DrawAll();
        }
        #endregion


        #region implement ICanvas =================================================================
        public IEnumerable<ISelectable> SelectableChildren
        {
            get
            {
                return UiChildrenById.Values;
            }
        }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale + _scroll + WindowPos;
        }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 screenPos)
        {
            return (screenPos - _scroll - WindowPos) / Scale;
        }


        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 TransformDirection(Vector2 vectorInCanvas)
        {
            return vectorInCanvas * Scale;
        }


        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 InverseTransformDirection(Vector2 vectorInScreen)
        {
            return vectorInScreen / Scale;
        }


        public ImRect TransformRect(ImRect canvasRect)
        {
            return new ImRect(TransformPosition(canvasRect.Min), TransformPosition(canvasRect.Max));
        }

        public ImRect InverseTransformRect(ImRect screenRect)
        {
            return new ImRect(InverseTransformPosition(screenRect.Min), InverseTransformPosition(screenRect.Max));
        }


        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale + _scroll;
        }
        #endregion


        #region public API
        public void DrawRect(ImRect rectOnCanvas, Color color)
        {
            GraphCanvas.DrawList.AddRect(TransformPosition(rectOnCanvas.Min), TransformPosition(rectOnCanvas.Max), color);
        }

        public void DrawRectFilled(ImRect rectOnCanvas, Color color)
        {
            GraphCanvas.DrawList.AddRectFilled(TransformPosition(rectOnCanvas.Min), TransformPosition(rectOnCanvas.Max), color);
        }
        #endregion

        /// <summary>
        /// The canvas that is currently being drawn from the UI.
        /// Note that <see cref="GraphCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Drawing() context.
        /// </summary>
        public static GraphCanvas Current { get; private set; }

        public static ImDrawListPtr DrawList;
        public Instance CompositionOp { get; set; }
        public Vector2 WindowPos;    // Position of the canvas window-panel within Application window
        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();

        #region private members ------
        private ImDrawListPtr _foreground;
        private Vector2 _size;
        private Vector2 _mouse;


        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);

        public float Scale = 1;            // The damped scale factor {read only}
        private float _scaleTarget = 1;

        private SelectionFence _selectionFence;
        private ImGuiIOPtr _io;
        private Dictionary<Guid, SymbolChildUi> UiChildrenById { get; set; }
        #endregion
    }
}
