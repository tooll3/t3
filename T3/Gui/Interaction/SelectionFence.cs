using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Gui.Selection;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the selection by dragging a fence
    /// </summary>
    public class SelectionFence
    {
        public SelectionFence(INodeCanvas canvas)
        {
            _canvas = canvas;
        }

        public void Draw()
        {
            if (_isDragging)
            {
                if (ImGui.IsMouseReleased(0))
                {
                    HandleDragCompleted();
                }
                else
                {
                    HandleDragDelta();
                }

                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(Bounds.Min, Bounds.Max, new Color(0.1f), 1);
            }
            else
            {
                if (!ImGui.IsAnyItemHovered() // Don't start dragging a fence if above an item or output
                    && ImGui.IsWindowHovered()
                    && ImGui.IsMouseClicked(0)
                    && !ImGui.GetIO().KeyAlt)
                {
                    HandleDragStarted();
                }
            }
        }

        private void HandleDragStarted()
        {
            var mouseMouse = ImGui.GetMousePos();
            _startPositionInScreen = mouseMouse;
            _dragPositionInScreen = mouseMouse;

            _isDragging = true;
        }

        private void HandleDragDelta()
        {
            _dragPositionInScreen = ImGui.GetMousePos();

            var boundsInCanvas = _canvas.InverseTransformRect(Bounds);

            var selectMode = SelectMode.Replace;
            if (ImGui.IsKeyPressed((int)Key.LeftShift))
            {
                selectMode = SelectMode.Add;
            }
            else if (ImGui.IsKeyPressed((int)Key.LeftCtrl))
            {
                selectMode = SelectMode.Remove;
            }

            if (!_dragThresholdExceeded)
            {
                if (_dragPositionInScreen == _startPositionInScreen)
                    return;

                _dragThresholdExceeded = true;
                if (selectMode == SelectMode.Replace)
                {
                    SelectionManager.Clear();
                }
            }

            var nodesToSelect = (from child in _canvas.SelectableChildren
                                 let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                                 where rect.Overlaps(boundsInCanvas)
                                 select child).ToList();

            SelectionManager.Clear();

            foreach (var node in nodesToSelect)
            {
                if (node is SymbolChildUi symbolChildUi)
                {
                    var instance = GraphCanvas.Current.CompositionOp.Children.FirstOrDefault(child => child.SymbolChildId == symbolChildUi.Id);
                    if (instance == null)
                    {
                        Log.Warning("Can't find instance");
                    }

                    switch (selectMode)
                    {
                        case SelectMode.Replace:
                        case SelectMode.Add:
                            SelectionManager.AddSelection(symbolChildUi, instance);
                            break;

                        case SelectMode.Remove:
                            SelectionManager.RemoveSelection(node);
                            break;
                    }
                }
                else
                {
                    switch (selectMode)
                    {
                        case SelectMode.Replace:
                        case SelectMode.Add:
                            SelectionManager.AddSelection(node);
                            break;

                        case SelectMode.Remove:
                            SelectionManager.RemoveSelection(node);
                            break;
                    }
                }
            }
        }

        private void HandleDragCompleted()
        {
            _dragThresholdExceeded = false;
            var newPosition = ImGui.GetMousePos();
            var delta = _startPositionInScreen - newPosition;
            var hasOnlyClicked = delta.LengthSquared() < 4f;
            if (hasOnlyClicked)
            {
                SelectionManager.Clear();
                SelectionManager.SetSelectionToParent(GraphCanvas.Current.CompositionOp);
            }

            _isDragging = false;
        }

        private enum SelectMode
        {
            Add = 0,
            Remove,
            Replace
        }

        private bool _isDragging;
        private ImRect Bounds => ImRect.RectBetweenPoints(_startPositionInScreen, _dragPositionInScreen);
        private Vector2 _startPositionInScreen;
        private Vector2 _dragPositionInScreen;
        private readonly INodeCanvas _canvas;
        private bool _dragThresholdExceeded; // Set to true after DragThreshold reached
    }
}