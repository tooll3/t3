using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeRange : IValueSnapAttractor
    {
        public void Draw(TimeLineCanvas canvas, Playback playback, ImDrawListPtr drawlist, ValueSnapHandler snapHandler)
        {
            if (playback == null)
                return;

            _playback = playback;

            ImGui.PushStyleColor(ImGuiCol.Button, TimeRangeMarkerColor.Rgba);

            // Range start
            {
                var xRangeStart = canvas.TransformPositionX((float)playback.TimeRangeStart);
                var rangeStartPos = new Vector2(xRangeStart, 0);

                // Shade outside
                drawlist.AddRectFilled(
                                       new Vector2(0, 0),
                                       new Vector2(xRangeStart, TimeRangeShadowSize.Y),
                                       TimeRangeOutsideColor);

                // Shadow
                drawlist.AddRectFilled(
                                       rangeStartPos - new Vector2(TimeRangeShadowSize.X - 1, 0),
                                       rangeStartPos + new Vector2(0, TimeRangeShadowSize.Y),
                                       TimeRangeShadowColor);

                // Line
                drawlist.AddRectFilled(rangeStartPos, rangeStartPos + new Vector2(1, 9999), TimeRangeShadowColor);

                SetCursorToBottom(
                                  xRangeStart - TimeRangeHandleSize.X,
                                  TimeRangeHandleSize.Y);

                ImGui.Button("##StartPos", TimeRangeHandleSize);

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    var newTime = canvas.InverseTransformPositionX(ImGui.GetIO().MousePos.X);
                    snapHandler.CheckForSnapping(ref newTime, new List<IValueSnapAttractor> {this});
                    playback.TimeRangeStart = newTime;
                }
            }

            // Range end
            {
                var rangeEndX = canvas.TransformPositionX((float)playback.TimeRangeEnd);
                var rangeEndPos = new Vector2(rangeEndX, 0);

                // Shade outside
                var windowMaxX = ImGui.GetContentRegionAvail().X + canvas.WindowPos.X;
                if (rangeEndX < windowMaxX)
                    drawlist.AddRectFilled(
                                           rangeEndPos,
                                           rangeEndPos + new Vector2(windowMaxX - rangeEndX, TimeRangeShadowSize.Y),
                                           TimeRangeOutsideColor);

                // Shadow
                drawlist.AddRectFilled(
                                       rangeEndPos,
                                       rangeEndPos + TimeRangeShadowSize,
                                       TimeRangeShadowColor);

                // Line
                drawlist.AddRectFilled(rangeEndPos, rangeEndPos + new Vector2(1, 9999), TimeRangeShadowColor);

                SetCursorToBottom(
                                  rangeEndX,
                                  TimeRangeHandleSize.Y);

                ImGui.Button("##EndPos", TimeRangeHandleSize);

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    //clipTime.TimeRangeEnd += canvas.InverseTransformDirection(ImGui.GetIO().MouseDelta).X;
                    var newTime = canvas.InverseTransformPositionX(ImGui.GetIO().MousePos.X);
                    snapHandler.CheckForSnapping(ref newTime, new List<IValueSnapAttractor> {this});
                    playback.TimeRangeEnd = newTime;
                }
            }

            ImGui.PopStyleColor();
        }

        private static void SetCursorToBottom(float xInScreen, float paddingFromBottom)
        {
            var max = ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos();
            var p = new Vector2(xInScreen, max.Y - paddingFromBottom);
            ImGui.SetCursorScreenPos(p);
        }

        private static readonly Vector2 TimeRangeHandleSize = new Vector2(10, 20);
        private static readonly Vector2 TimeRangeShadowSize = new Vector2(5, 9999);
        private static readonly Color TimeRangeShadowColor = new Color(0, 0, 0, 0.5f);
        private static readonly Color TimeRangeOutsideColor = new Color(0.0f, 0.0f, 0.0f, 0.3f);
        private static readonly Color TimeRangeMarkerColor = new Color(1f, 1, 1f, 0.3f);

        #region implement snapping interface -----------------------------------
        private static Playback _playback;

        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime)
        {
            if (_playback == null)
                return null;
            
            const float snapDistance = 4;
            var snapThresholdOnCanvas = TimeLineCanvas.Current.InverseTransformDirection(new Vector2(snapDistance, 0)).X;
            SnapResult bestSnapResult = null;

            KeyframeOperations.CheckForBetterSnapping(targetTime, _playback.TimeRangeStart, snapThresholdOnCanvas, ref bestSnapResult);
            KeyframeOperations.CheckForBetterSnapping(targetTime, _playback.TimeRangeEnd, snapThresholdOnCanvas, ref bestSnapResult);
            return bestSnapResult;
        }
        #endregion
    }
}