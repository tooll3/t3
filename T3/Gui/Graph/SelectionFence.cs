using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the selection by dragging a fence
    /// </summary>
    public class SelectionFence
    {
        public SelectionFence(ICanvas canvas)
        {
            _canvas = canvas;
            _selectionHandler = canvas.SelectionHandler;
        }


        public void Draw()
        {
            if (!_isVisible)
            {
                if (!ImGui.IsAnyItemHovered()   // Don't start dragging a fence if above an item or output
                    && ImGui.IsWindowHovered()
                    && ImGui.IsMouseClicked(0))
                {
                    HandleDragStarted();
                }
            }
            else
            {
                if (!ImGui.IsMouseReleased(0))
                {
                    HandleDragDelta();
                }
                else
                {
                    HandleDragCompleted();
                }

                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(Bounds.Min, Bounds.Max, new Color(0.1f), 1);
            }

            if (ImGui.IsKeyPressed((int)Key.Delete))
            {
                // TODO: Implement Key Detection and delete command
                Console.WriteLine("Would delete stuff:" + _selectionHandler.SelectedElements);
            }
        }


        public void HandleDragStarted()
        {
            var mouseMouse = ImGui.GetMousePos();
            _startPositionInScreen = mouseMouse;
            _dragPositionInScreen = mouseMouse;

            _isVisible = true;
        }


        public void HandleDragDelta()
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
                    _selectionHandler?.Clear();
                }
            }

            if (_selectionHandler != null)
            {
                var elementsToSelect = (from child in _canvas.SelectableChildren
                                        let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                                        where rect.Overlaps(boundsInCanvas)
                                        select child).ToList();

                switch (selectMode)
                {
                    case SelectMode.Add:
                        _selectionHandler.AddElements(elementsToSelect);
                        break;

                    case SelectMode.Remove:
                        _selectionHandler.RemoveElements(elementsToSelect);
                        break;

                    case SelectMode.Replace:
                        _selectionHandler.SetElements(elementsToSelect);
                        break;
                }
            }
        }

        public void HandleDragCompleted()
        {
            _dragThresholdExceeded = false;
            var newPosition = ImGui.GetMousePos();
            var delta = _startPositionInScreen - newPosition;
            var hasOnlyClicked = delta.LengthSquared() < 4f;
            if (hasOnlyClicked)
            {
                _selectionHandler.Clear();
            }
            _isVisible = false;
        }


        private enum SelectMode
        {
            Add = 0,
            Remove,
            Replace
        }

        private readonly SelectionHandler _selectionHandler;
        private bool _isVisible;
        private ImRect Bounds => ImRect.RectBetweenPoints(_startPositionInScreen, _dragPositionInScreen);
        private Vector2 _startPositionInScreen;
        private Vector2 _dragPositionInScreen;
        private readonly ICanvas _canvas;
        private bool _dragThresholdExceeded; // Set to true after DragThreshold reached
    }
}
