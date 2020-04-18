using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    public class ClipRange : IValueSnapAttractor
    {
        public void Draw(TimeLineCanvas canvas, ITimeClip timeClip, ImDrawListPtr drawlist, ValueSnapHandler snapHandler)
        {
            if (timeClip == null)
                return;

            _timeClip = timeClip;
            
            //_playback = playback;

            ImGui.PushStyleColor(ImGuiCol.Button, TimeRangeMarkerColor.Rgba);

            // Range start
            {
                var xRangeStart = canvas.TransformX(timeClip.SourceRange.Start);
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
                    var newTime = canvas.InverseTransformX(ImGui.GetIO().MousePos.X);
                    snapHandler.CheckForSnapping(ref newTime, canvas.Scale.X, new List<IValueSnapAttractor> {this});
                    var delta = newTime - timeClip.SourceRange.Start;
                    var speed =  timeClip.TimeRange.Duration / timeClip.SourceRange.Duration;
                    timeClip.SourceRange.Start = newTime;
                    timeClip.TimeRange.Start += delta * speed;
                }
            }

            // Range end
            {
                var rangeEndX = canvas.TransformX(timeClip.SourceRange.End);
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
                    var newTime = canvas.InverseTransformX(ImGui.GetIO().MousePos.X);
                    snapHandler.CheckForSnapping(ref newTime, canvas.Scale.X, new List<IValueSnapAttractor> {this});
                    var delta = newTime - timeClip.SourceRange.End;
                    var speed =  timeClip.TimeRange.Duration / timeClip.SourceRange.Duration;
                    timeClip.SourceRange.End = newTime;
                    timeClip.TimeRange.End += delta * speed;
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

        //private static Playback _playback;
        private static ITimeClip _timeClip;
        #region implement snapping interface -----------------------------------

        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            if (_timeClip == null)
                return null;
            
            SnapResult bestSnapResult = null;

            ValueSnapHandler.CheckForBetterSnapping(targetTime, _timeClip.SourceRange.Start, canvasScale, ref bestSnapResult);
            ValueSnapHandler.CheckForBetterSnapping(targetTime, _timeClip.SourceRange.End, canvasScale, ref bestSnapResult);
            return bestSnapResult;
        }
        #endregion
    }
}