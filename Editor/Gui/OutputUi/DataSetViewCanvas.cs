using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.DataSet;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.TimeLine.Raster;

namespace T3.Editor.Gui.OutputUi;

/// <summary>
/// Provides a simple canvas to visualize a <see cref="DataSet"/>.
/// </summary>
public class DataSetViewCanvas
{
    public void Draw(DataSet dataSet)
    {
        if (dataSet == null)
            return;

        // Very ugly hack to prevent scaling the output above window size
        var keepScale = T3Ui.UiScaleFactor;
        T3Ui.UiScaleFactor = 1;
        DrawCanvas();
        T3Ui.UiScaleFactor = keepScale;
        return;

        void DrawCanvas()
        {
            var currentTime = Playback.RunTimeInSecs;
            ImGui.BeginChild("Scrollable", Vector2.Zero, false, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
            if (ShowInteraction)
            {
                ImGui.Checkbox("Filter Recent ", ref OnlyRecentEvents);
                ImGui.SameLine();
                ImGui.Checkbox("Scroll ", ref Scroll);
                ImGui.SameLine(0, 20);
                if (ImGui.Button("Export as JSON"))
                {
                    dataSet.WriteToFile();
                }
            }

            _standardRaster.Draw(_canvas);
            _canvas.UpdateCanvas();

            if (Scroll)
            {
                var windowWidth = ImGui.GetWindowWidth();
                var speed = 100f;
                _canvas.SetVisibleRangeHard(new Vector2(speed, -1), new Vector2((float)currentTime - (windowWidth - 4) / speed, 0));
            }

            var dl = ImGui.GetWindowDrawList();
            var min = ImGui.GetWindowPos();
            var max = ImGui.GetContentRegionAvail() + min;

            var visibleMinTime = _canvas.InverseTransformX(min.X);
            var visibleMaxTime = _canvas.InverseTransformX(max.X);

            const int maxVisibleEvents = 1000;
            const float layerHeight = 20f;
            var plotCurvePoints = new Vector2[maxVisibleEvents];

            _pathTreeDrawer.Reset();

            // Draw hovered time
            if (ImGui.IsWindowHovered())
            {
                var mousePos = ImGui.GetMousePos();
                dl.AddRectFilled(new Vector2(mousePos.X, min.Y), new Vector2(mousePos.X + 1, max.Y), UiColors.WidgetActiveLine.Fade(0.1f));
            }

            var filterRecentEventDuration = 10;
            var dataSetChannels = dataSet.Channels.OrderBy(c => string.Join((string)".", (IEnumerable<string>)c.Path));

            foreach (var channel in dataSetChannels)
            {
                var newVisibleRange = new ValueRange() { Min = float.PositiveInfinity, Max = float.NegativeInfinity };
                var pathString = string.Join(" / ", channel.Path);
                var channelHash = pathString.GetHashCode();

                // Compute random unique color
                var foreGroundBrightness = UiColors.ForegroundFull.V;
                var randomHue = (Math.Abs(channelHash) % 357) / 360f;
                var randomSaturation = (channelHash % 13) / 13f / 3f + 0.4f;
                var randomChannelColor = Color.FromHSV(randomHue, randomSaturation, foreGroundBrightness, 1);

                _channelValueRanges.TryGetValue(channelHash, out var valueRange);

                if (OnlyRecentEvents)
                {
                    var lastEvent = channel.GetLastEvent();
                    var tooOld = (lastEvent == null || lastEvent.Time < currentTime - filterRecentEventDuration);
                    if (tooOld)
                        continue;
                }

                var isVisible = _pathTreeDrawer.DrawEntry(channel.Path, MaxTreeLevel);
                if (!isVisible)
                    continue;

                var layerMin = ImGui.GetCursorScreenPos();
                var layerMax = new Vector2(max.X, layerMin.Y + layerHeight);
                var layerHovered = ImGui.IsMouseHoveringRect(layerMin, layerMax);

                if (layerHovered)
                {
                    dl.AddRectFilled(layerMin, layerMax, UiColors.BackgroundFull.Fade(0.1f));
                }

                var visibleEventCount = 0;
                var visiblePlotPointCount = 0;

                var lastX = float.PositiveInfinity;

                // binary search index with highest visible time
                var lastVisibleIndex = channel.FindHighestIndexBelowTime(visibleMaxTime);

                // ReSharper disable once ForCanBeConvertedToForeach
                // Draw events starting from the end (left to right)
                for (var index = lastVisibleIndex;
                     index >= 0
                     && index >= lastVisibleIndex - maxVisibleEvents
                     && visibleEventCount < maxVisibleEvents;
                     index--)
                {
                    var dataEvent = channel.Events[index];
                    var msg = dataEvent.Value == null ? "NULL" : dataEvent.Value.ToString();

                    float markerYInLayer;
                    var value = float.NaN;

                    if (dataEvent.TryGetNumericValue(out var numericValue))
                    {
                        value = (float)numericValue;
                        var fNormalized = ((value - valueRange.Min) / (valueRange.Max - valueRange.Min)).Clamp(0, 1);
                        markerYInLayer = (1 - fNormalized) * (layerHeight - 2) + 2;
                        msg = $"{value:G4}";
                    }
                    else
                    {
                        markerYInLayer = layerHeight - 3;
                    }

                    float xStart;

                    // Draw interval events
                    if (dataEvent is DataIntervalEvent intervalEvent)
                    {
                        if (!(intervalEvent.EndTime > visibleMinTime) || !(intervalEvent.Time < visibleMaxTime))
                        {
                            continue;
                        }

                        xStart = _canvas.TransformX((float)intervalEvent.Time);

                        var endTime = intervalEvent.IsUnfinished ? currentTime : intervalEvent.EndTime;
                        var xEnd = MathF.Max(_canvas.TransformX((float)endTime), xStart + 1);

                        dl.AddRectFilled(new Vector2(xStart, layerMin.Y + markerYInLayer),
                                         new Vector2(xEnd, layerMax.Y),
                                         randomChannelColor.Fade(0.3f));

                        if (ImGui.IsMouseHoveringRect(new Vector2(xStart, layerMin.Y), new Vector2(xEnd, layerMax.Y)))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{dataEvent.Time:0.000s} ... {endTime:0.000s} ->  {msg}");
                            ImGui.EndTooltip();
                        }
                    }
                    // Draw value events
                    else
                    {
                        if (!(dataEvent.Time > visibleMinTime) || !(dataEvent.Time < visibleMaxTime))
                            continue;

                        xStart = _canvas.TransformX((float)dataEvent.Time);
                        var y = layerMin.Y + markerYInLayer;
                        dl.AddTriangleFilled(new Vector2(xStart, y - 3),
                                             new Vector2(xStart + 2.5f, y + 2),
                                             new Vector2(xStart - 2.5f, y + 2),
                                             randomChannelColor);

                        plotCurvePoints[visiblePlotPointCount++] = new Vector2(xStart, y);

                        if (ImGui.IsMouseHoveringRect(new Vector2(xStart, layerMin.Y), new Vector2(xStart + 2, layerMax.Y)))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{dataEvent.Time:0.000s} -> {msg}");
                            ImGui.EndTooltip();
                        }
                    }

                    // Draw label if enough space
                    var gapSize = lastX - xStart;
                    var estimatedWidth = msg.Length * Fonts.FontSmall.FontSize * 0.6f;

                    if (!string.IsNullOrEmpty(msg) && gapSize > 20)
                    {
                        var fade = MathUtils.RemapAndClamp(gapSize, estimatedWidth - 30, estimatedWidth + 60, 0, 1);
                        dl.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize,
                                   new Vector2(xStart + 8, layerMin.Y + 3),
                                   randomChannelColor.Fade(0.6f * fade), msg);
                    }

                    if (!float.IsNaN(value))
                    {
                        newVisibleRange.Min = MathF.Min(newVisibleRange.Min, value);
                        newVisibleRange.Max = MathF.Max(newVisibleRange.Max, value);
                    }

                    lastX = xStart;
                    visibleEventCount++;
                }

                // Adjust auto height of plot line
                var newRange = new ValueRange(MathUtils.Lerp(valueRange.Min, newVisibleRange.Min, 0.1f),
                                              MathUtils.Lerp(valueRange.Max, newVisibleRange.Max, 0.1f));
                if (float.IsNaN(newRange.Min) || float.IsNaN(newRange.Max))
                    newRange = new ValueRange();

                _channelValueRanges[channelHash] = newRange;

                // Draw plot line
                if (visiblePlotPointCount > 1)
                {
                    dl.AddPolyline(ref plotCurvePoints[0], visiblePlotPointCount, randomChannelColor.Fade(0.2f), ImDrawFlags.None, 1);
                }

                // Draw label flashing with recent events
                dl.AddRectFilled(layerMin - new Vector2(200, 0), // fill tree indentation
                                 layerMin + new Vector2(200, layerHeight),
                                 UiColors.WindowBackground.Fade(0.9f)
                                );

                dl.AddRectFilledMultiColor(layerMin + new Vector2(200, 0),
                                           layerMin + new Vector2(400, layerHeight),
                                           UiColors.WindowBackground.Fade(0.9f),
                                           UiColors.WindowBackground.Fade(0.0f),
                                           UiColors.WindowBackground.Fade(0.0f),
                                           UiColors.WindowBackground.Fade(0.9f)
                                          );

                var lastEventAgeFactor = MathF.Pow((float)(currentTime - channel.GetLastEvent().Time).Clamp(0, 1) / 1, 0.2f);
                var label = channel.Path.Last();
                if (!string.IsNullOrEmpty(label))
                    dl.AddText(Fonts.FontBold,
                               Fonts.FontBold.FontSize,
                               layerMin + new Vector2(10, 0),
                               Color.Mix(UiColors.ForegroundFull, randomChannelColor.Fade(0.5f), lastEventAgeFactor),
                               label);

                // Line below layer
                dl.AddLine(new Vector2(layerMin.X, layerMax.Y), layerMax, UiColors.GridLines.Fade(0.2f));

                layerMin.Y += layerHeight;
                layerMax.Y += layerHeight;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + layerHeight);
            }

            ImGui.Dummy(Vector2.One);
            _pathTreeDrawer.Complete();

            // Draw current time
            var xTime = _canvas.TransformX((float)currentTime);
            dl.AddRectFilled(new Vector2(xTime, min.Y), new Vector2(xTime + 1, max.Y), UiColors.WidgetActiveLine);
            dl.PopClipRect();
            ImGui.EndChild();
            ImGui.SetCursorPos(Vector2.Zero);
        }
    }

    private struct ValueRange
    {
        public ValueRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min;
        public float Max;
    }

    private readonly Dictionary<int, ValueRange> _channelValueRanges = new();

    public bool OnlyRecentEvents = true;
    public bool Scroll = true;
    public bool ShowInteraction = true;
    public int MaxTreeLevel = 2;

    private readonly PathTreeDrawer _pathTreeDrawer = new();
    private readonly StandardValueRaster _standardRaster = new() { EnableSnapping = true };

    private readonly ScalableCanvas _canvas = new(isCurveCanvas: true)
                                                  {
                                                      FillMode = ScalableCanvas.FillModes.FillAvailableContentRegion,
                                                  };
}