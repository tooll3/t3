using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction.Snapping;

namespace T3.Editor.Gui.Windows.TimeLine
{
    /// <summary>
    /// A graphic representation that allows to move and scale multiple selected timeline elements
    /// </summary>
    internal class TimeSelectionRange : IValueSnapAttractor
    {
        public TimeSelectionRange(TimeLineCanvas timeLineCanvas, ValueSnapHandler snapHandler)
        {
            _timeLineCanvas = timeLineCanvas;
            _snapHandler = snapHandler;
        }

        public void Draw(Instance composition, ImDrawListPtr drawlist)
        {
            if (!_isDragging && !ImGui.GetIO().KeyAlt)
                return;
            
            _selectionTimeRange = _timeLineCanvas.GetSelectionTimeRange();
            if (!_selectionTimeRange.IsValid || _selectionTimeRange.Duration <= 0)
                return;

            var contentRegionMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
            var contentRegionMax = ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos();
            ImGui.PushStyleColor(ImGuiCol.Button, TimeRangeMarkerColor.Rgba);
            // Range start
            {
                var xRangeStartOnScreen = _timeLineCanvas.TransformX(_selectionTimeRange.Start);
                var rangeStartPos = new Vector2(xRangeStartOnScreen, contentRegionMin.Y);
                // Shade outside
                drawlist.AddRectFilled(
                                       new Vector2(0, 0),
                                       new Vector2(xRangeStartOnScreen, TimeRangeShadowSize.Y),
                                       TimeRangeOutsideColor);

                // Shadow
                drawlist.AddRectFilled(
                                       rangeStartPos - new Vector2(TimeRangeShadowSize.X - 1, 0),
                                       rangeStartPos + new Vector2(0, TimeRangeShadowSize.Y),
                                       TimeRangeShadowColor);

                // Line
                drawlist.AddRectFilled(rangeStartPos, rangeStartPos + new Vector2(1, 9999), TimeRangeShadowColor);
                
                ImGui.SetCursorScreenPos(rangeStartPos 
                                         + new Vector2(-TimeRangeHandleSize.X, 
                                                       (contentRegionMax-contentRegionMin).Y - TimeRangeHandleSize.Y));
                ImGui.Button("##SelectionStartPos", TimeRangeHandleSize);

                HandleDrag(composition, _selectionTimeRange.Start, _selectionTimeRange.End);
            }

            // Range end
            {
                var xRangeEndOnScreen = _timeLineCanvas.TransformX(_selectionTimeRange.End);
                var rangeEndPos = new Vector2(xRangeEndOnScreen, contentRegionMin.Y);

                // Shade outside
                //var windowMaxX =  ImGui.GetContentRegionAvail().X + _timeLineCanvas.WindowPos.X;
                if (xRangeEndOnScreen < contentRegionMax.X)
                    drawlist.AddRectFilled(
                                           rangeEndPos,
                                           new Vector2(contentRegionMax.X, TimeRangeShadowSize.Y),
                                           TimeRangeOutsideColor);

                // Shadow
                drawlist.AddRectFilled(
                                       rangeEndPos,
                                       rangeEndPos + TimeRangeShadowSize,
                                       TimeRangeShadowColor);

                // Line
                drawlist.AddRectFilled(rangeEndPos, rangeEndPos + new Vector2(1, 9999), TimeRangeShadowColor);

                ImGui.SetCursorScreenPos(rangeEndPos 
                                         + new Vector2(0, (contentRegionMax-contentRegionMin).Y - TimeRangeHandleSize.Y));
                
                ImGui.Button("##SelectionEndPos", TimeRangeHandleSize);
                HandleDrag(composition, _selectionTimeRange.End, _selectionTimeRange.Start);
            }

            ImGui.PopStyleColor();
        }

        private void HandleDrag(Instance composition, double originalU, double origin)
        {
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var u = _timeLineCanvas.InverseTransformX(ImGui.GetIO().MousePos.X);

                if (!_isDragging)
                {
                    _timeLineCanvas.StartDragCommand();
                    _lastDragU = originalU;
                    _isDragging = true;
                }

                if(!ImGui.GetIO().KeyShift)
                    _snapHandler.CheckForSnapping(ref u, _timeLineCanvas.Scale.X, new List<IValueSnapAttractor> { this });
                
                var dScale = (u - origin) / (_lastDragU - origin);
                _timeLineCanvas.UpdateDragStretchCommand(scaleU: dScale, scaleV: 1, originU: origin, originV: 0);
                _lastDragU = u;
            }
            else if (ImGui.IsItemDeactivated() && _isDragging)
            {
                _isDragging = false;
                _timeLineCanvas.CompleteDragCommand();
            }
        }

        private bool _isDragging;
        private double _lastDragU;

        private static void SetCursorToBottom(float xInScreen, float paddingFromBottom)
        {
            var max = ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos();
            var p = new Vector2(xInScreen, max.Y - paddingFromBottom);
            ImGui.SetCursorScreenPos(p);
        }

        #region implement snapping interface -----------------------------------
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            SnapResult bestSnapResult = null;

            ValueSnapHandler.CheckForBetterSnapping(targetTime, _selectionTimeRange.Start, canvasScale, ref bestSnapResult);
            ValueSnapHandler.CheckForBetterSnapping(targetTime, _selectionTimeRange.End, canvasScale, ref bestSnapResult);
            return bestSnapResult;
        }
        #endregion

        private static readonly Vector2 TimeRangeHandleSize = new(10, 20);
        private static readonly Vector2 TimeRangeShadowSize = new(5, 9999);
        private static readonly Color TimeRangeShadowColor = new(0, 0, 0, 0.4f);
        private static readonly Color TimeRangeOutsideColor = new(0.0f, 0.0f, 0.0f, 0.2f);
        private static readonly Color TimeRangeMarkerColor = new(1f, 1, 1f, 0.3f);
        private readonly TimeLineCanvas _timeLineCanvas;
        private readonly ValueSnapHandler _snapHandler;
        private TimeRange _selectionTimeRange;
    }
}