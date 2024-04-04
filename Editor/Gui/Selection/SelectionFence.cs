using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.SystemUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.SystemUi;

namespace T3.Editor.Gui.Selection
{
    /// <summary>
    /// Provides fence selection interaction
    /// </summary>
    public sealed class SelectionFence
    {
        internal States UpdateAndDraw(out SelectModes selectMode, bool allowRectOutOfBounds = true)
        {
            if (_state is States.CompletedAsArea or States.CompletedAsClick)
                _state = States.Inactive;
            
            var globalMouse = EditorUi.Instance.Cursor;

            var io = ImGui.GetIO();
            var imguiMousePos = io.MousePos;
            selectMode = GetSelectMode(io);
            var isLeftButtonDown = globalMouse.IsButtonDown(MouseButtons.Left);
            
            const ImGuiHoveredFlags hoverRules = ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.ChildWindows;
            
            if (_state == States.Inactive)
            {
                if (ImGui.IsAnyItemHovered()
                    || !ImGui.IsWindowHovered(hoverRules)
                    || !isLeftButtonDown
                    || ImGui.GetIO().KeyAlt
                    || FrameStats.Last.IsItemContextMenuOpen)
                {
                    return States.Inactive;
                }

                _startPositionInScreen = imguiMousePos;
                _state = States.PressedButNotMoved;
                return States.PressedButNotMoved;
            }

            var globalMousePos = globalMouse.PositionVec;
            var interactionMin = Vector2.Max(_startPositionInScreen, globalMousePos);
            var interactionMax = Vector2.Min(_startPositionInScreen, globalMousePos);
            
            var minContentRegion = ImGui.GetWindowContentRegionMin();
            var maxContentRegion = ImGui.GetWindowContentRegionMax();
            var onScreenMin = Vector2.Max(minContentRegion, interactionMin);
            var onScreenMax = Vector2.Min(maxContentRegion, interactionMax);
            
            BoundsInScreen = ImRect.RectBetweenPoints(onScreenMin, onScreenMax);
            BoundsUnclamped = ImRect.RectBetweenPoints(_startPositionInScreen, globalMousePos);

            if (!isLeftButtonDown) // check for release - even if released off-window
            {
                _state = _state == States.PressedButNotMoved ? States.CompletedAsClick : States.CompletedAsArea;
                return _state;
            }

            var clickThreshold = UserSettings.Config.ClickThreshold;
            
            var totalPositionDelta = globalMousePos - _startPositionInScreen;
            if (_state == States.PressedButNotMoved && totalPositionDelta.LengthSquared() < clickThreshold * clickThreshold)
                return _state;

            var drawList = ImGui.GetWindowDrawList();
            var drawRect = allowRectOutOfBounds ? BoundsUnclamped : BoundsInScreen;
            drawList.AddRectFilled(drawRect.Min, drawRect.Max, new Color(0.1f), 1);
            drawList.AddRect(drawRect.Min - Vector2.One, drawRect.Max + Vector2.One, new Color(0,0,0, 0.4f), 1);
            _state = States.Updated;
            return _state;

            static SelectModes GetSelectMode(in ImGuiIOPtr io)
            {
                if (io.KeyShift)
                {
                    return SelectModes.Add;
                }

                if (io.KeyCtrl)
                {
                    return SelectModes.Remove;
                }

                return SelectModes.Replace;
            }
        }

        public enum SelectModes
        {
            Add = 0,
            Remove,
            Replace
        }

        public enum States
        {
            Inactive,
            PressedButNotMoved,
            Updated,
            CompletedAsArea,
            CompletedAsClick,
        }

        internal States State => _state;
        private States _state;

        internal ImRect BoundsInScreen;
        internal ImRect BoundsUnclamped;
        private Vector2 _startPositionInScreen;
    }
}