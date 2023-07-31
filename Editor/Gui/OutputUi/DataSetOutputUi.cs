using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.DataSet;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows.TimeLine.Raster;

namespace T3.Editor.Gui.OutputUi
{
    public class DataSetOutputUi : OutputUi<float>
    {
        public override IOutputUi Clone()
        {
            return new DataSetOutputUi()
                       {
                           OutputDefinition = OutputDefinition,
                           PosOnCanvas = PosOnCanvas,
                           Size = Size
                       };
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is not Slot<DataSet> dataSetSlot)
                return;

            //DrawDataSet(dataSetSlot.Value);
            Draw(dataSetSlot.Value);
        }

        private void Draw(DataSet dataSet)
        {
            if (dataSet == null)
                return;
            
            var dl = ImGui.GetWindowDrawList();
            var min = ImGui.GetWindowPos();
            var max = ImGui.GetContentRegionAvail() + min;
            
            _canvas.UpdateCanvas();

            var currentTime = Playback.RunTimeInSecs;
            
            
            dl.PushClipRect(_canvas.WindowPos, _canvas.WindowPos + _canvas.WindowSize, true);
            //_raster.Draw(_canvas);
            _standardRaster.Draw(_canvas);

            var visibleMinTime = _canvas.InverseTransformX(min.X);
            var visibleMaxTime = _canvas.InverseTransformX(max.X);

            
            Vector2 layerMin = min;
            var layerHeight = 20f;
            Vector2 layerMax = new Vector2(max.X, min.Y + layerHeight);

            var dataSetChannels = dataSet.Channels.OrderBy(c => string.Join(".", c.Path));
            foreach (var channel in dataSetChannels)
            {
                
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var index = 0; index < channel.Events.Count; index++)
                {
                    var dataEvent = channel.Events[index];
                    if (dataEvent.Value is not float f)
                        continue;
                    
                    var height = (f / 127) * layerHeight + 2;
                    
                    if (dataEvent is DataIntervalEvent intervalEvent)
                    {
                        if (!(intervalEvent.EndTime > visibleMinTime) || !(intervalEvent.Time < visibleMaxTime))
                        {
                            continue;
                        }

                        var xStart = _canvas.TransformX((float)intervalEvent.Time);
                        
                        var endTime = intervalEvent.IsUnfinished ? currentTime : intervalEvent.EndTime;
                        var xEnd =MathF.Max(_canvas.TransformX((float)endTime), xStart + 1);
                        
                        dl.AddRectFilled(new Vector2(xStart, layerMin.Y + height), new Vector2(xEnd, layerMax.Y), UiColors.TextMuted);
                        if(ImGui.IsMouseHoveringRect(new Vector2(xStart, layerMin.Y), new Vector2(xEnd, layerMax.Y)))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"Time: {dataEvent.Time:0.000s} ... {endTime:0.000s}\nValue: {f:0.00}");
                            ImGui.EndTooltip();
                        }
                    }
                    else
                    {
                        var xStart = _canvas.TransformX((float)dataEvent.Time);
                        var y = layerMin.Y + height;
                        dl.AddRectFilled(new Vector2(xStart, y), new Vector2(xStart + 2, y+2), Color.Blue);
                        
                        if(ImGui.IsMouseHoveringRect(new Vector2(xStart, layerMin.Y), new Vector2(xStart+2, layerMax.Y)))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"Time: {dataEvent.Time:0.000s}\nValue: {f:0.00}");
                            ImGui.EndTooltip();
                        }
                    }
                }

                var pathString = string.Join(" / ", channel.Path);
                dl.AddText(layerMin + new Vector2(10,0), Color.White, pathString);

                layerMin.Y += layerHeight;
                layerMax.Y += layerHeight;
            }

            var xTime = _canvas.TransformX((float)currentTime);
            dl.AddRectFilled(new Vector2(xTime, min.Y), new Vector2(xTime+1, max.Y), UiColors.WidgetActiveLine);
            dl.PopClipRect();
        }
        
        private readonly HorizontalRaster _raster = new();
        private readonly StandardValueRaster _standardRaster = new() { EnableSnapping = true };
        private readonly ScalableCanvas _canvas = new(isCurveCanvas:true)
                                                      {
                                                          FillMode = ScalableCanvas.FillModes.FillAvailableContentRegion,
                                                      };

        public static void DrawDataSet(DataSet dataSet)
        {
            if (dataSet == null)
                return;

            foreach (var channel in dataSet.Channels)
            {
                ImGui.SetNextItemOpen(true);
                ImGui.TreeNode(string.Join(" / ", channel.Path));

                var lastEvent = channel.GetLastEvent();
                ImGui.TextUnformatted($"{channel.Events.Count}  {lastEvent.Value}");
                ImGui.TreePop();
            }
        }
    }
}