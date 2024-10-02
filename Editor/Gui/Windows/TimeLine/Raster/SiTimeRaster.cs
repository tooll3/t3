using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Animation;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.TimeLine.Raster
{
    /// <summary>
    /// A <see cref="AbstractTimeRaster"/> that displays Seconds, Minutes, Hours, etc. 
    /// </summary>
    public class SiTimeRaster : AbstractTimeRaster
    {
        public override void Draw(Playback playback, float unitsPerSeconds)
        {
            if (ScaleRanges == null || Math.Abs(UserSettings.Config.TimeRasterDensity - _initializedDensity) > 0.0001f)
            {
                ScaleRanges = InitializeTimeScaleDefinitions(UserSettings.Config.TimeRasterDensity * 0.02f);
                _initializedDensity = UserSettings.Config.TimeRasterDensity;
            }

            var scale = TimeLineCanvas.Current.Scale.X * playback.Bpm / 120f;
            var scroll = TimeLineCanvas.Current.Scroll.X / playback.Bpm * 120f;

            DrawTimeTicks(scale, scroll, TimeLineCanvas.Current);
        }

        private const double Epsilon = 0.01f;

        private static string Format(double t, double spacing, int modulo)
        {
            var l = (int)(t / spacing  + 0.0001f) % modulo;
            if (t < 0)
            {
                l--;
            }
            
            return ""+l;
        }

        protected override string BuildLabel(Raster raster, double beatTime)
        {
            var time = beatTime / BarsToSecs;
            var output = "";
            foreach (var c in raster.Label)
            {
                output += c switch
                              {
                                  'Y' => Format(time, everyYear, 9999),
                                  'D' => Format(time, everyDay, 365),
                                  'H' => Format(time, everyHour, 24),
                                  'M' => Format(time, everyMinute, 60),
                                  'S' => Format(time, everySec, 60),
                                  'F' => Format(time, everySec / 60, 60),
                                  'T' => "." + Format(time, every100Ms, 10),
                                  _   => c
                              };
            }

            return output;
        }

        public override SnapResult CheckForSnap(double time, float canvasScale)
        {
            return ImGui.GetIO().KeyAlt 
                       ? base.CheckForSnap(time, canvasScale) 
                       : null;
        }

        
        private const float BarsToSecs = 0.5f;
        private float _initializedDensity;

        const float everyYear = 365 * 24 * 60 * 60;
        const float every10Days = 10 * 24 * 60 * 60;
        const float everyDay = 24 * 60 * 60;
        const float every4Hours = 4* 60 * 60;
        const float everyHour = 60 * 60;
        const float every5Minute = 5 * 60;
        const float everyMinute = 1 * 60;
        const float every15Sec = 15;
        const float everySec = 1;
        const float every100Ms = 1 / 10f;
        const float every10Ms = 1 / 100f;
        
        private static List<ScaleRange> InitializeTimeScaleDefinitions(float density)
        {
            density *= 2f;

            var scales = new List<ScaleRange>
                             {
                                 new()
                                     {
                                         ScaleMax = 0.0002 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = everySec * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "T", Spacing = every100Ms * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "", Spacing = every10Ms * BarsToSecs, FadeLabels = true, FadeLines = true },
                                                       }
                                     },

                                 new()
                                     {
                                         ScaleMax = 0.0010 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = everySec * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "T", Spacing = every100Ms * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 0.005 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = everySec * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "", Spacing = every100Ms * BarsToSecs, FadeLabels = true, FadeLines = true },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 0.01 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = every15Sec * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = everySec * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 0.03 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = every15Sec * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "", Spacing = everySec * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 0.1 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Ss", Spacing = every15Sec * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                           new() { Label = "", Spacing = everySec * BarsToSecs, FadeLabels = true, FadeLines = true }
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 0.5 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "", Spacing = every15Sec * BarsToSecs, FadeLabels = true, FadeLines = true },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 1 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = every5Minute * BarsToSecs, FadeLabels = false, FadeLines = true },
                                                           new() { Label = "Mm", Spacing = everyMinute * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 5 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Mm", Spacing = every5Minute * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                           new() { Label = "", Spacing = everyMinute * BarsToSecs, FadeLabels = true, FadeLines = true },
                                                       }
                                     }, 
                                 new()
                                     {
                                         ScaleMax = 20 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Hh", Spacing = every4Hours * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Hh", Spacing = everyHour * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                       }
                                     },
                                 
                                 new()
                                     {
                                         ScaleMax = 100 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Dd", Spacing = everyDay * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Hh", Spacing = every4Hours * BarsToSecs, FadeLabels = true, FadeLines = true },
                                                       }
                                     },

                                 new()
                                     {
                                         ScaleMax = 500 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Dd", Spacing = every10Days * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "Dd", Spacing = everyDay * BarsToSecs, FadeLabels = true, FadeLines = false },
                                                       }
                                     },

                                 new()
                                     {
                                         ScaleMax = 750 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Dd", Spacing = every10Days * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                           new() { Label = "", Spacing = everyDay * BarsToSecs, FadeLabels = false, FadeLines = true },
                                                       }
                                     },
                                 new()
                                     {
                                         ScaleMax = 9999 / density,
                                         Rasters = new List<Raster>
                                                       {
                                                           new() { Label = "Dd", Spacing = every10Days * BarsToSecs, FadeLabels = false, FadeLines = false },
                                                       }
                                     },
                             };

            var minScale = 0.0;
            foreach (var s in scales)
            {
                s.ScaleMin = minScale;
                minScale = s.ScaleMax;
            }

            return scales;
        }
    }
}