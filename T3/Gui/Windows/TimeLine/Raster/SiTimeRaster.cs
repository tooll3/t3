using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine.Raster
{
    /// <summary>
    /// A <see cref="AbstractTimeRaster"/> that displays Seconds, Minutes, Hours, etc. 
    /// </summary>
    public class SiTimeRaster : AbstractTimeRaster
    {
        public override void Draw(Playback playback)
        {
            //var hasChanged = Math.Abs(1 - playback.Bpm) > 0.001f;
            if (ScaleRanges == null)
            {
                ScaleRanges = InitializeTimeScaleDefinitions();
            }

            var scale = TimeLineCanvas.Current.NestedTimeScale;
            var scroll = TimeLineCanvas.Current.NestedTimeOffset;

            DrawTimeTicks(scale, -scroll / scale, TimeLineCanvas.Current);
        }

        private const double Epsilon = 0.01f;
        protected override string BuildLabel(Raster raster, double beatTime)
        {
            
            var time = beatTime / BeatTimeFactor;
            var output = "";
            foreach (char c in raster.Label)
            {
                // days
                if (c == 'D')
                {
                    var days = (int)(time / 60 / 60 /24  + Epsilon);
                    output += $"{days}";
                }                
                // hours
                else if (c == 'H')
                {
                    var hours = (int)(time / 60 / 60 + Epsilon) % 24;
                    output += $"{hours}";
                }

                // minutes
                else if (c == 'M')
                {
                    var minutes = (int)(time / 60 + Epsilon) % 60;
                    output += $"{minutes}";
                }

                // seconds
                else if (c == 'S')
                {
                    var seconds = (int)(time + Epsilon) % 60;
                    output += $"{seconds}";
                }
                // frames
                else if (c == 'F')
                {
                    var frames = (int)(time * 60 + Epsilon) % 60;
                    output += $"{frames}.";
                }
                else
                {
                    output += c;
                }
            }

            return output;
        }

        public override  SnapResult CheckForSnap(double time, float canvasScale)
        {
            if (ImGui.GetIO().KeyAlt)
            {
                var xxx=base.CheckForSnap(time, canvasScale);
                return xxx;
            }
            
            return null;
        }  
        
        //private new const float Density = 1f;
        private const float BeatTimeFactor = 0.5f;
        private static List<ScaleRange> InitializeTimeScaleDefinitions()
        {
            return new List<ScaleRange>
                       {
                           // frames 
                           new ScaleRange()
                               {
                                   ScaleMin = 0.0000 / Density,
                                   ScaleMax = 0.0002 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Ss", Spacing = 1 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Ff", Spacing = 1/60f * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = false
                                                         },                                                     
                                                 }
                               }, 
                           // frames lines
                           new ScaleRange()
                               {
                                   ScaleMin = 0.0002 / Density,
                                   ScaleMax = 0.005 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Ss", Spacing = 1 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "", Spacing = 1/60f * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = true
                                                         },                                                     
                                                 }
                               },                            
                           // 1sec
                           new ScaleRange()
                               {
                                   ScaleMin = 0.005 / Density,
                                   ScaleMax = 0.01 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Ss", Spacing = 1 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                 }
                               },                                     
                           
                           // 1sec
                           new ScaleRange()
                               {
                                   ScaleMin = 0.01 / Density,
                                   ScaleMax = 0.03 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Ss", Spacing = 15 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "", Spacing = 1 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },                                                     
                                                 }
                               },                           
                           // 1min
                           new ScaleRange()
                               {
                                   ScaleMin = 0.03 / Density,
                                   ScaleMax = 0.1 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Ss", Spacing = 15 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "", Spacing = 1 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = true
                                                         }                                                     
                                                 }
                               },
                           // 5min
                           new ScaleRange()
                               {
                                   ScaleMin = 0.1 / Density,
                                   ScaleMax = 0.5 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                 }
                               },
                           // 20min 
                           new ScaleRange()
                               {
                                   ScaleMin = 0.5 / Density,
                                   ScaleMax = 1 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 5 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = true
                                                         },                                                       
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = false
                                                         },
                                                 }
                               },
                           // 1h
                           new ScaleRange()
                               {
                                   ScaleMin = 1 / Density,
                                   ScaleMax = 4 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "Mm", Spacing = 5 * 60 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = true
                                                         },                                                     
                                                     new Raster()
                                                         {
                                                             Label = "", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = true
                                                         },
                                                 }
                               },                           // 1h
                           new ScaleRange()
                               {
                                   ScaleMin = 4 / Density,
                                   ScaleMax = 20 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },
                                                     new Raster()
                                                         {
                                                             Label = "", Spacing = 1 * 60 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = true
                                                         },
                                                 }
                               },
                           // 10h
                           new ScaleRange()
                               {
                                   ScaleMin = 20 / Density,
                                   ScaleMax = 100 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Dd", Spacing = 24 * 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },                                                     
                                                     
                                                     new Raster()
                                                         {
                                                             Label = "Hh", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = false
                                                         },
                                                 }
                               },

                           // 1d
                           new ScaleRange()
                               {
                                   ScaleMin = 100 / Density,
                                   ScaleMax = 9999 / Density,
                                   Rasters = new List<Raster>
                                                 {
                                                     new Raster()
                                                         {
                                                             Label = "Dd", Spacing = 24 * 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = false, FadeLines = false
                                                         },                                                     
                                                     new Raster()
                                                         {
                                                             Label = "", Spacing = 60 * 60 * BeatTimeFactor,
                                                             FadeLabels = true, FadeLines = true
                                                         },
                                                 }
                               },
                       };
        }
        
  
    }
}