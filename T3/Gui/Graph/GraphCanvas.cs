using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
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
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            DrawList = ImGui.GetWindowDrawList();
            _io = ImGui.GetIO();


            ImGui.BeginGroup();
            {
                _mouse = ImGui.GetMousePos();

                WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
                WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);


                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);
                // Damp scaling
                Scale = Im.Lerp(Scale, _scaleTarget, _io.DeltaTime * 20);
                Scroll = Im.Lerp(Scroll, _scrollTarget, _io.DeltaTime * 20);

                HandleInteraction();

                DrawGrid();
                GraphRendering.DrawGraph();

                if (BuildingConnections.TempConnection != null && ImGui.IsMouseReleased(0))
                {
                    BuildingConnections.Cancel();
                }

                _selectionFence.Draw();
                DrawList.PopClipRect();
                DrawContextMenu();
            }
            ImGui.EndGroup();
        }

        public List<Instance> GetParents(bool includeCompositionOp = false)
        {
            var parents = new List<Instance>();
            var op = CompositionOp;
            if (includeCompositionOp)
                parents.Add(op);

            while (op.Parent != null)
            {
                op = op.Parent;
                parents.Insert(0, op);
            }

            return parents;
        }


        public IEnumerable<Symbol> GetParentSymbols()
        {
            return GetParents(includeCompositionOp: true).Select(p => p.Symbol);
        }

        private void HandleInteraction()
        {
            if (!ImGui.IsWindowHovered())
                return;

            if (ImGui.IsMouseDragging(1))
            {
                _scrollTarget += _io.MouseDelta;
            }

            if (!ImGui.IsAnyItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), CompositionOp.Symbol, InverseTransformPosition(ImGui.GetMousePos()));
            }

            HandleZoomInteraction();

            ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
        }


        private void HandleZoomInteraction()
        {
            if (_io.MouseWheel == 0)
                return;

            const float zoomSpeed = 1.2f;
            var focusCenter = (_mouse - Scroll - WindowPos) / Scale;


            var zoomDelta = 1f;

            if (_io.MouseWheel < 0.0f)
            {
                for (float zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomDelta /= zoomSpeed;
                }
            }

            if (_io.MouseWheel > 0.0f)
            {
                for (float zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomDelta *= zoomSpeed;
                }
            }
            _scaleTarget *= zoomDelta;

            Vector2 shift = _scrollTarget + (focusCenter * _scaleTarget);
            _scrollTarget += _mouse - shift - WindowPos;
        }

        private bool _contextMenuIsOpen = false;
        private void DrawContextMenu()
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                Vector2 scene_pos = ImGui.GetMousePosOnOpeningCurrentPopup();// - scrollOffset;
                _contextMenuIsOpen = true;

                // Todo: Convert to linc
                var selectedChildren = new List<SymbolChildUi>();
                foreach (var x in SelectionHandler.SelectedElements)
                {
                    if (x is SymbolChildUi childUi)
                    {
                        selectedChildren.Add(childUi);
                    }
                }

                if (selectedChildren.Count > 0)
                {
                    var label = selectedChildren.Count == 1
                        ? $"{selectedChildren[0].SymbolChild.ReadableName} Item..." : $"{selectedChildren.Count} Items...";

                    ImGui.Text(label);
                    if (ImGui.MenuItem(" Rename..", null, false, false)) { }

                    if (ImGui.MenuItem(" Delete", null))
                    {
                        var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                        var cmd = new DeleteSymbolChildCommand(compositionSymbolUi, selectedChildren);
                        UndoRedoStack.AddAndExecute(cmd);
                    }

                    if (ImGui.MenuItem(" Copy", null, false, false)) { }
                    ImGui.Separator();
                }
                if (ImGui.MenuItem("Rename..", null, false, false)) { }
                if (ImGui.MenuItem("Add"))
                {
                    QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), CompositionOp.Symbol, InverseTransformPosition(ImGui.GetMousePos()));
                }
                if (ImGui.MenuItem("Paste", null, false, false)) { }
                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }
            ImGui.PopStyleVar();
        }


        private void DrawGrid()
        {
            var gridSize = 64.0f * Scale.X;
            for (float x = Scroll.X % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(
                    new Vector2(x, 0.0f) + WindowPos,
                    new Vector2(x, WindowSize.Y) + WindowPos,
                    new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }

            for (float y = Scroll.Y % gridSize; y < WindowSize.Y; y += gridSize)
            {
                DrawList.AddLine(
                    new Vector2(0.0f, y) + WindowPos,
                    new Vector2(WindowSize.X, y) + WindowPos,
                    new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }
        }


        #endregion


        #region implement ICanvas =================================================================
        public IEnumerable<ISelectable> SelectableChildren
        {
            get
            {
                _selectableItems.Clear();
                _selectableItems.AddRange(ChildUis);
                var symbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                _selectableItems.AddRange(symbolUi.InputUis.Values);
                _selectableItems.AddRange(symbolUi.OutputUis.Values);

                return _selectableItems;
            }
        }

        private readonly List<ISelectable> _selectableItems = new List<ISelectable>();

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale + Scroll + WindowPos;
        }

        /// <summary>
        /// Convert at screen space position (e.g. from mouse) to canvas coordinates applying canvas zoom and scrolling 
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 screenPos)
        {
            return (screenPos - Scroll - WindowPos) / Scale;
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
        /// Transform a canvas position to relative position within imgui-window (e.g. to set ImGui context) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale + Scroll;
        }
        #endregion


        #region public API
        public void DrawRect(ImRect rectOnCanvas, Color color)
        {
            GraphCanvas.Current.DrawList.AddRect(TransformPosition(rectOnCanvas.Min), TransformPosition(rectOnCanvas.Max), color);
        }

        public void DrawRectFilled(ImRect rectOnCanvas, Color color)
        {
            GraphCanvas.Current.DrawList.AddRectFilled(TransformPosition(rectOnCanvas.Min), TransformPosition(rectOnCanvas.Max), color);
        }

        /// <summary>
        /// The canvas that is currently being drawn from the UI.
        /// Note that <see cref="GraphCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Drawing() context.
        /// </summary>
        public static GraphCanvas Current { get; private set; }

        public ImDrawListPtr DrawList { get; private set; }
        public Instance CompositionOp { get; set; }

        /// <summary>
        /// Position of the canvas window-panel within Application window
        /// </summary>
        public Vector2 WindowPos { get; private set; }
        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();

        /// <summary>
        /// The damped scale factor {read only}
        /// </summary>
        public Vector2 Scale { get; set; } = Vector2.One;
        private Vector2 _scaleTarget = Vector2.One;

        public Vector2 Scroll { get; private set; } = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);
        #endregion


        #region private members ------
        //private ImDrawListPtr _foreground;
        public Vector2 WindowSize { get; private set; }
        private Vector2 _mouse;

        private SelectionFence _selectionFence;
        private ImGuiIOPtr _io;
        internal static Vector2 DefaultOpSize = new Vector2(100, 30);

        internal List<SymbolChildUi> ChildUis { get; set; }
        private BuildingNodes _buildingNodes = new BuildingNodes();
        #endregion
    }
}
