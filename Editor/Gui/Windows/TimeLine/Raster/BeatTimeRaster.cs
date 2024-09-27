using System.Text;
using T3.Core.Animation;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.TimeLine.Raster;

/// <summary>
/// A <see cref="AbstractTimeRaster"/> that displays Bars, Beats and ticks 
/// </summary>
public class BeatTimeRaster : AbstractTimeRaster
{
    public override void Draw(Playback playback, float unitsPerSeconds)
    {
        var hasChanged = Math.Abs(_lastRasterBpm - playback.Bpm) > 0.001f;
        if (ScaleRanges == null || hasChanged)
        {
            _lastRasterBpm = playback.Bpm;
            ScaleRanges = InitializeTimeScaleDefinitions(UserSettings.Config.TimeRasterDensity * 0.02f);
        }

        var scale = TimeLineCanvas.Current.Scale.X;
        var scroll = TimeLineCanvas.Current.Scroll.X;
        DrawTimeTicks(scale, scroll, TimeLineCanvas.Current);
    }

    protected override string BuildLabel(Raster raster, double timeInBars)
    {
        _stringBuilder.Clear();

        foreach (char c in raster.Label)
        {
            switch (c)
            {
                // bars
                case 'b':
                    _stringBuilder.Append($"{(int)timeInBars}");
                    break; 
                // beats
                case '.':
                    _stringBuilder.Append($".{(int)(timeInBars * 4) % 4}");
                    break; 
                // ticks
                case ':':
                    _stringBuilder.Append($":{(int)(timeInBars * 16) % 4}");
                    break; 
                default:
                    _stringBuilder.Append(c);
                    break;
            }
        }

        return _stringBuilder.ToString();
    }

    private static List<ScaleRange> InitializeTimeScaleDefinitions(float density)
    {
        const float everyTick = 1 / 240f * 60 / 4;
        const float everyBeat = 1 / 240f * 60;
        const float everyBar = 4 / 240f * 60;
        const float everyMeasure = 16 / 240f * 60;
        const float everyPhrase = 64 / 240f * 60;
        const float every10Phrases = 10 * 64 / 240f * 60;

            
        var scales = new List<ScaleRange>
                         {
                             new()
                                 {
                                     ScaleMax = 0.001 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "b.", Spacing = everyBeat, FadeLabels = false, FadeLines = false },
                                                       new() { Label = ":", Spacing = everyTick, FadeLabels = true, FadeLines = false },
                                                   }
                                 },
                                 
                             new()
                                 {
                                     ScaleMax = 0.002 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "b.", Spacing = everyBeat, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "", Spacing = everyTick, FadeLabels = false, FadeLines = true },
                                                   }
                                 },

                             new()
                                 {
                                     ScaleMax = 0.005 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "b.", Spacing = everyBeat, FadeLabels = true, FadeLines = false },
                                                   },
                                 },

                             new()
                                 {
                                     ScaleMax = 0.007 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "", Spacing = everyBeat, FadeLabels = true, FadeLines = false },
                                                   },
                                 },

                             new()
                                 {
                                     ScaleMax = 0.01 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "", Spacing = everyBeat, FadeLabels = true, FadeLines = true },
                                                   },
                                 },

                                 
                             new()
                                 {
                                     ScaleMax = 0.012 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = false, FadeLines = false },
                                                   },
                                 },
                             new()
                                 {
                                     ScaleMax = 0.03 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyMeasure, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "b", Spacing = everyBar, FadeLabels = true, FadeLines = false },
                                                   },
                                 },
                             new()
                                 {
                                     ScaleMax = 0.06 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyMeasure, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "", Spacing = everyBar, FadeLabels = true, FadeLines = true },
                                                   },
                                 },
                             new()
                                 {
                                     ScaleMax = 0.1 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyMeasure, FadeLabels = false, FadeLines = false },
                                                   },
                                 },
                                 
                             new()
                                 {
                                     ScaleMax = 0.15 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyPhrase, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "b", Spacing = everyMeasure, FadeLabels = true, FadeLines = false },
                                                           
                                                   },
                                 },                                 
                             new()
                                 {
                                     ScaleMax = 0.3 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = everyPhrase, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "", Spacing = everyMeasure, FadeLabels = true, FadeLines = true },
                                                           
                                                   },
                                 },                                 
                             new()
                                 {
                                     ScaleMax = 0.5 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = every10Phrases, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "b", Spacing = everyPhrase, FadeLabels = true, FadeLines = false },
                                                   },
                                 },
                                 
                             new()
                                 {
                                     ScaleMax = 1.2 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = every10Phrases, FadeLabels = false, FadeLines = false },
                                                       new() { Label = "", Spacing = everyPhrase, FadeLabels = true, FadeLines = true },
                                                   },
                                 },                                 

                             new()
                                 {
                                     ScaleMax = 2 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "b", Spacing = every10Phrases, FadeLabels = true, FadeLines = false },
                                                   },
                                 },     
                                 
                             new()
                                 {
                                     ScaleMax = 999 / density,
                                     Rasters = new List<Raster>
                                                   {
                                                       new() { Label = "", Spacing = every10Phrases, FadeLabels = false, FadeLines = false },
                                                   },
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

    private double _lastRasterBpm = 240;
    private static readonly StringBuilder _stringBuilder = new(20);
}