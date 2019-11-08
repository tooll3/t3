using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Handles the selection by dragging a fence
    /// </summary>
    public class TimeSelectionFence
    {
        public TimeSelectionFence(TimeLineCanvas canvas)
        {
            _timeLineCanvas = canvas;
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
                //Console.WriteLine("Would delete stuff:" + _selectionHandler.SelectedElements);
            }
        }


        private void HandleDragStarted()
        {
            var mouseMouse = ImGui.GetMousePos();
            _startPositionInScreen = mouseMouse;
            _dragPositionInScreen = mouseMouse;

            _isVisible = true;
        }


        private void HandleDragDelta()
        {
            _dragPositionInScreen = ImGui.GetMousePos();

            var selectMode = SelectMode.Replace;
            if (ImGui.GetIO().KeyShift)
            {
                selectMode = SelectMode.Add;
            }
            else if (ImGui.GetIO().KeyCtrl)
            {
                selectMode = SelectMode.Remove;
            }
            _timeLineCanvas.UpdateSelectionForArea(Bounds, selectMode);
        }

        private void HandleDragCompleted()
        {
            _isVisible = false;
        }

        private bool _isVisible;
        private ImRect Bounds => ImRect.RectBetweenPoints(_startPositionInScreen, _dragPositionInScreen);
        private Vector2 _startPositionInScreen;
        private Vector2 _dragPositionInScreen;
        private readonly TimeLineCanvas _timeLineCanvas;
    }
}
