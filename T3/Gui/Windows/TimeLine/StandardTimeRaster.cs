using System;
using System.Collections.Generic;
using System.Globalization;
using T3.Core.Animation;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// A time raster that calculate required labels and spacing logarithmically. 
    /// </summary>
    public class StandardTimeRaster : TimeRaster
    {
        public override void Draw(Playback playback)
        {
            var unitInSecs = UnitsPerSecond * Playback.Bpm / 240f;
            DrawTimeTicks(TimeLineCanvas.Current.Scale.X / unitInSecs, TimeLineCanvas.Current.Scroll.X * unitInSecs);
        }
        
        public float UnitsPerSecond { get; set; } = 1;
        
        protected override string BuildLabel(Raster raster, double time)
        {
            var output = "";
            foreach (var c in raster.Label)
            {
                if (c == 'N')
                {
                    output += time.ToString("G5", CultureInfo.InvariantCulture);
                }
                else
                {
                    output += c;
                }
            }
            return output;
        }

        
        protected override IEnumerable<Raster> GetRastersForScale(double scale, out float fadeFactor)
        {
            const float density = 1.0f;
            var uPerPixel = scale;
            var logScale = (float)Math.Log10(uPerPixel) + density;
            var logScaleMod = (logScale + 1000)%1.0f;
            var logScaleFloor = (float)Math.Floor(logScale);
            
            if (logScaleMod < 0.5)
            {
                fadeFactor = 1-logScaleMod*2;
                _blendRasters[0]=new Raster()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor)*50,
                                    };
                _blendRasters[1]= new Raster()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor)*10,
                                        FadeLabels = true,
                                        FadeLines = true,
                                    };
            }
            else
            {
                fadeFactor = 1-(logScaleMod - 0.5f)*2;
                _blendRasters[0]= new Raster()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor)*100,
                                    };
                _blendRasters[1]= new Raster()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor)*50,
                                        FadeLabels = true,
                                        FadeLines = true,
                                    };
            }
            return _blendRasters;
        }
        
        private readonly Raster[] _blendRasters = new Raster[2];
    }
}