using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Input;
using T3.Gui.Selection;

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
            if (!isVisible)
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
                drawList.AddRectFilled(_bounds.Min, _bounds.Max, new Color(0.5f), 1);
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

            isVisible = true;
        }


        public void HandleDragDelta()
        {
            _dragPositionInScreen = ImGui.GetMousePos();
            var delta = _startPositionInScreen - _dragPositionInScreen;

            var boundsInCanvas = GraphCanvas.Current.InverseTransformRect(_bounds);

            var _selectMode = SelectMode.Replace;
            if (ImGui.IsKeyPressed((int)Key.LeftShift))
            {
                _selectMode = SelectMode.Add;
            }
            else if (ImGui.IsKeyPressed((int)Key.LeftCtrl))
            {
                _selectMode = SelectMode.Remove;
            }


            if (!_dragThresholdExceeded)
            {
                if (_dragPositionInScreen == _startPositionInScreen)
                    return;

                _dragThresholdExceeded = true;
                if (_selectMode == SelectMode.Replace)
                {
                    if (_selectionHandler != null)
                        _selectionHandler.Clear();
                }
            }

            if (_selectionHandler != null)
            {
                List<ISelectable> elementsToSelect = new List<ISelectable>();
                foreach (var child in _canvas.SelectableChildren)
                {
                    var selectableWidget = child as ISelectable;
                    if (selectableWidget != null)
                    {
                        var rect = new ImRect(child.Position, child.Position + child.Size);
                        if (rect.Overlaps(boundsInCanvas))
                        {
                            elementsToSelect.Add(selectableWidget);
                        }
                    }
                }

                switch (_selectMode)
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
            isVisible = false;
        }


        private enum SelectMode
        {
            Add = 0,
            Remove,
            Replace,
        }

        private SelectionHandler _selectionHandler;

        private bool isVisible = false;
        ImRect _bounds { get { return ImRect.RectBetweenPoints(_startPositionInScreen, _dragPositionInScreen); } }
        private Vector2 _startPositionInScreen;
        private Vector2 _dragPositionInScreen;
        private ICanvas _canvas;
        private bool _dragThresholdExceeded = false; // Set to true after DragThreshold reached
    }
}
