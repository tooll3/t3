using System;
using System.Collections.Generic;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// A <see cref="TimeRaster"/> that displays Bars, Beats and ticks 
    /// </summary>
    public class BeatTimeRaster : TimeRaster
    {
        public override void Draw(Playback playback)
        {
            var hasChanged = Math.Abs(_bpm - playback.Bpm) > 0.001f;
            if (ScaleRanges == null || hasChanged)
            {
                //_bpm = playback.Bpm;
                ScaleRanges= InitializeTimeScaleDefinitions();
            }
            DrawTimeTicks(TimeLineCanvas.Current.Scale.X, TimeLineCanvas.Current.Scroll.X );
        }

        private double _bpm = 240;

        protected override string BuildLabel(Raster raster, double timeInBars)
        {
            var output = "";
            foreach (char c in raster.Label)
            {
                // bars
                if (c == 'b')
                {
                    //var bars = (int)(time*_bpm/60f/4f) + 1;
                    var bars = (int)(timeInBars) + 1;
                    output += $"{bars}.";
                }
                // beats
                else if (c == '.')
                {
                    var beats = (int)(timeInBars*4)%4 + 1;
                    output += $".{beats}";
                }
                // ticks
                else if (c == ':')
                {
                    var ticks = (int)(timeInBars*16)%4 + 1;
                    output += $":{ticks}";
                }
                else
                {
                    output += c;
                }
            }

            return output;
        }

        private List<ScaleRange> InitializeTimeScaleDefinitions()
        {
            return new List<ScaleRange>
                         {
                             // 0
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 1600,
                                 ScaleMax = _bpm / 900,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b",  Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b.",  Spacing = 1 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "", 
                                                           Spacing = 1 / _bpm * 60 / 4, FadeLabels = false,
                                                           FadeLines = true
                                                       },
                                                   }
                             },

                             // 1
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 900,
                                 ScaleMax = _bpm / 700,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b",  Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b.",  Spacing = 1 / _bpm * 60,
                                                           FadeLabels = true, FadeLines = true
                                                       },
                                                   },
                             },

                             // 2
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 700,
                                 ScaleMax = _bpm / 300,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b",  Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                   },
                             },
                             // 3
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 300,
                                 ScaleMax = _bpm / 200,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b",  Spacing = 4 / _bpm * 60,
                                                           FadeLabels = true, FadeLines = false
                                                       },
                                                   },
                             },
                             // 4
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 200,
                                 ScaleMax = _bpm / 100,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "",  Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = true
                                                       },
                                                   },
                             },
                             // 5
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 100,
                                 ScaleMax = _bpm / 50,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                   }
                             },
                             // 6
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 50,
                                 ScaleMax = _bpm / 20,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                   }
                             },
                             // 7
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 20,
                                 ScaleMax = _bpm / 8,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 16 / _bpm * 60,
                                                           FadeLabels = true, FadeLines = true
                                                       },
                                                       //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                                       //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                                   }
                             },
                             // 8
                             new ScaleRange()
                             {
                                 ScaleMin = _bpm / 8,
                                 ScaleMax = _bpm / 1,
                                 Rasters = new List<Raster>
                                                   {
                                                       new Raster()
                                                       {
                                                           Label = "b", Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       //new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=true  },
                                                       //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                                       //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                                   }
                             },
                         };
            
        }
    }}