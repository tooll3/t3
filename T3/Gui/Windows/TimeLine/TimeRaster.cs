using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Interaction.Snapping;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public abstract class TimeRaster : IValueSnapAttractor
    {
        public abstract void Draw(ClipTime clipTime);
        protected abstract string BuildLabel(Raster raster, double time);

        
        protected virtual IEnumerable<Raster> GetRastersForScale(double scale, out float fadeFactor)
        {
            var scaleRange = ScaleRanges.FirstOrDefault(range => range.ScaleMax*Density > scale);
            fadeFactor = scaleRange == null 
                             ? 1
                             : 1-(float)Im.Remap(scale, scaleRange.ScaleMin*Density, scaleRange.ScaleMax*Density, 0, 1);
            
            return scaleRange?.Rasters;
        }
        
        
        protected void DrawTimeTicks(double scale, double scroll)
        {
            if (!(scale > Epsilon))
                return;

            var drawList = ImGui.GetWindowDrawList();
            var topLeft = TimeLineCanvas.Current.WindowPos;
            var viewHeight = TimeLineCanvas.Current.WindowSize.Y;
            var width = TimeLineCanvas.Current.WindowSize.X;

            _usedPositions.Clear();

            scale = 1/scale;

            var rasters = GetRastersForScale(scale, out var fadeFactor);

            if (rasters == null)
                return;
            
            // Debug string 
            // drawList.AddText(topLeft + new Vector2(20, 20), Color.Red, $"Scale: {pixelsPerU:0.1}  f={scaleRange:0}");

            foreach (var raster in rasters)
            {
                double t = -scroll%raster.Spacing;


                var lineAlpha = raster.FadeLines ? fadeFactor : 1;
                var lineColor = new Color(0, 0, 0, lineAlpha*0.3f);

                var textAlpha = raster.FadeLabels ? fadeFactor : 1;
                var textColor = new Color(textAlpha);

                while (t/scale < width)
                {
                    var xIndex = (int)(t/scale);

                    if (xIndex > 0 && xIndex < width && !_usedPositions.ContainsKey(xIndex))
                    {
                        _usedPositions[xIndex] = t + scroll;

                        drawList.AddRectFilled(
                                         new Vector2(topLeft.X + xIndex, topLeft.Y),
                                         new Vector2(topLeft.X + xIndex+1, topLeft.Y + viewHeight), lineColor);

                        if (raster.Label != "")
                        {
                            var time = t + scroll;
                            var output = BuildLabel(raster, time);

                            var p = topLeft + new Vector2(xIndex, viewHeight - 15);
                            drawList.AddText(p, textColor, output);
                        }
                    }

                    t += raster.Spacing;
                }
            }
        }

        #region implement snap attractor
        private const double SnapThreshold = 6;

        public SnapResult CheckForSnap(double time)
        {
            foreach (var beatTime in _usedPositions.Values)
            {
                var distanceToTime = Math.Abs(time - beatTime)*TimeLineCanvas.Current.Scale.X;
                if (distanceToTime < SnapThreshold)
                {
                    return new SnapResult(beatTime, SnapThreshold - distanceToTime);
                }
            }

            return null;
        }
        #endregion

        private readonly Dictionary<int, double> _usedPositions = new Dictionary<int, double>();
        protected List<ScaleRange> ScaleRanges;
        private const float Density = 0.02f;
        private const double Epsilon = 0.001f;

        public class ScaleRange
        {
            public double ScaleMin { get; set; }
            public double ScaleMax { get; set; }
            public List<Raster> Rasters { get; set; }
        }
        
        public struct Raster
        {
            public string Label { get; set; }
            public double Spacing { get; set; }
            public bool FadeLabels { get; set; }
            public bool FadeLines { get; set; }
        }
    }
}