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
                ImGui.Checkbox("Recent ", ref OnlyRecentEvents);
                ImGui.SameLine();
                ImGui.Checkbox("Scroll ", ref Scroll);
                ImGui.SameLine();
                if (ImGui.Button("Save"))
                {
                    dataSet.WriteToFile();
                }
            }
            
            _standardRaster.Draw(_canvas);
            _canvas.UpdateCanvas();

            if (Scroll)
            {
                var windowWidth = ImGui.GetWindowWidth();
                var speed = 50f;
                _canvas.SetVisibleRangeHard(new Vector2(speed,-1 ), new Vector2((float)currentTime- (windowWidth-4)/speed,0));
            }
            
            var dl = ImGui.GetWindowDrawList();
            var min = ImGui.GetWindowPos();
            var max = ImGui.GetContentRegionAvail() + min;

            var visibleMinTime = _canvas.InverseTransformX(min.X);
            var visibleMaxTime = _canvas.InverseTransformX(max.X);

            const float layerHeight = 20f;

            _pathTreeDrawer.Reset();

            var filterRecentEventDuration = 10;
            var dataSetChannels = dataSet.Channels.OrderBy(c => string.Join((string)".", (IEnumerable<string>)c.Path));

            foreach (var channel in dataSetChannels)
            {
                var newVisibleRange = new ValueRange() {Min = float.PositiveInfinity, Max = float.NegativeInfinity};
                _channelValueRanges.TryGetValue(channel.GetHashCode(), out var valueRange);
            
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

                double lastEventTime = 0;
                var visibleEventCounts = 0;
                const int maxVisibleEvents = 500;
                
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var index = channel.Events.Count - 1; index >= 0
                                                           && index >= channel.Events.Count - maxVisibleEvents
                                                           && visibleEventCounts < maxVisibleEvents; index--)
                {
                    var dataEvent = channel.Events[index];
                    var msg = dataEvent.Value.ToString();
        
                    var height = 1 * (layerHeight -2);
                    var value = float.NaN;
                    if (dataEvent is { Value: float f })
                    {
                        value = f;
                        var fNormalized = 0f;
                        if (!float.IsNaN(f))
                        {
                            fNormalized = ((f-valueRange.Min) / (valueRange.Max - valueRange.Min)).Clamp(0,1);
                        }
                    
                        height = (1-fNormalized) * (layerHeight -2) ;
                        msg = $"{f:0.00}";
                    }

                
                    if (dataEvent is DataIntervalEvent intervalEvent)
                    {
                        lastEventTime = intervalEvent.EndTime;
                        if (!(intervalEvent.EndTime > visibleMinTime) || !(intervalEvent.Time < visibleMaxTime))
                        {
                            continue;
                        }

                        var xStart = _canvas.TransformX((float)intervalEvent.Time);
                        
                        var endTime = intervalEvent.IsUnfinished ? currentTime : intervalEvent.EndTime;
                        var xEnd =MathF.Max(_canvas.TransformX((float)endTime), xStart + 1);
                        
                        dl.AddRectFilled(new Vector2(xStart, layerMin.Y + height), new Vector2(xEnd, layerMax.Y), UiColors.StatusAutomated);
                        if(ImGui.IsMouseHoveringRect(new Vector2(xStart, layerMin.Y), new Vector2(xEnd, layerMax.Y)))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{dataEvent.Time:0.000s} ... {endTime:0.000s} ->  {msg}");
                            ImGui.EndTooltip();
                        }
                    }
                    else
                    {
                        if (!(dataEvent.Time > visibleMinTime) || !(dataEvent.Time < visibleMaxTime))
                            continue;
                    
                        var xStart = _canvas.TransformX((float)dataEvent.Time);
                        var y = layerMin.Y + height;
                        dl.AddRectFilled(new Vector2(xStart, y), new Vector2(xStart + 2, y+2), UiColors.StatusAutomated);
                        
                        if(ImGui.IsMouseHoveringRect(new Vector2(xStart, layerMin.Y), new Vector2(xStart+2, layerMax.Y)))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{dataEvent.Time:0.000s} -> {msg}");
                            ImGui.EndTooltip();
                        }
                        lastEventTime = dataEvent.Time;
                    }

                    if (!float.IsNaN(value))
                    {
                        newVisibleRange.Min = MathF.Min(newVisibleRange.Min, value);
                        newVisibleRange.Max = MathF.Max(newVisibleRange.Max, value);
                    }
                    visibleEventCounts++;
                }

            
                var newRange = new ValueRange(MathUtils.Lerp(valueRange.Min, newVisibleRange.Min, 0.1f),
                                              MathUtils.Lerp(valueRange.Max, newVisibleRange.Max, 0.1f));
                if (float.IsNaN(newRange.Min) || float.IsNaN(newRange.Max))
                    newRange = new ValueRange();
            
                _channelValueRanges[channel.GetHashCode()] = newRange; 

                var timeSinceEvent = MathF.Pow((float)(currentTime - lastEventTime).Clamp(0,10)/10,0.25f);
                var color = Color.Mix(UiColors.StatusAnimated, UiColors.TextMuted, (float)timeSinceEvent);

                var pathString = string.Join(" / ", channel.Path);
                if(!string.IsNullOrEmpty(pathString))
                    dl.AddText(layerMin + new Vector2(10,0),  color, pathString);
            
                dl.AddLine(new Vector2(layerMin.X, layerMax.Y), layerMax, UiColors.GridLines.Fade(0.4f));

                layerMin.Y += layerHeight;
                layerMax.Y += layerHeight;
                ImGui.SetCursorPosY( ImGui.GetCursorPosY() + layerHeight );
            }
            ImGui.Dummy(Vector2.One);
            _pathTreeDrawer.Complete();

            var xTime = _canvas.TransformX((float)currentTime);
            dl.AddRectFilled(new Vector2(xTime, min.Y), new Vector2(xTime+1, max.Y), UiColors.WidgetActiveLine);
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
    private readonly ScalableCanvas _canvas = new(isCurveCanvas:true)
                                                  {
                                                      FillMode = ScalableCanvas.FillModes.FillAvailableContentRegion,
                                                  };
}