using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction.Snapping;

internal class ValueSnapHandler
{
    private const float SnapIndicatorDuration = 1;

    public void AddSnapAttractor(IValueSnapAttractor sp)
    {
        if (!_snapAttractors.Contains(sp))
        {
            _snapAttractors.Add(sp);
        }
    }

    public void RemoveSnapAttractor(IValueSnapAttractor sp)
    {
        if (_snapAttractors.Contains(sp))
        {
            _snapAttractors.Remove(sp);
        }
    }

    /// <summary>
    /// Components can bind to these events to render snap-indicators
    /// </summary>
    public event Action<double> SnappedEvent;

        
    /// <summary>
    /// Override to float precision 
    /// </summary>
    public bool CheckForSnapping(ref float time, float canvasScale, List<IValueSnapAttractor> ignoreSnapAttractors = null)
    {
        double d = time;
        var result = CheckForSnapping(ref d, canvasScale, ignoreSnapAttractors);
        if (result)
            time = (float)d;

        return result;
    }

    /// <summary>
    /// Uses all registered snap providers to test for snapping
    /// </summary>
    public bool CheckForSnapping(ref double time, float canvasScale, List<IValueSnapAttractor> ignoreSnapAttractors = null)
    {
        var bestSnapValue = Double.NaN;
        double maxSnapForce = 0;
        foreach (var sp in _snapAttractors)
        {
            if (ignoreSnapAttractors != null && ignoreSnapAttractors.Contains(sp))
                continue;

            var snapResult = sp.CheckForSnap(time, canvasScale);
            if (snapResult != null && snapResult.Force > maxSnapForce)
            {
                bestSnapValue = snapResult.SnapToValue;
                maxSnapForce = snapResult.Force;
            }
        }

        if (!Double.IsNaN(bestSnapValue))
        {
            SnappedEvent?.Invoke(bestSnapValue);
            _lastSnapTime = ImGui.GetTime();
            _lastSnapPosition = bestSnapValue;
        }

        if (Double.IsNaN(bestSnapValue))
            return false;

        time = bestSnapValue;
        return true;
    }


    /// <summary>
    /// This is method is called from all snapHandlers 
    /// </summary>
    public static bool CheckForBetterSnapping(double targetTime, double anchorTime, float canvasScale, ref SnapResult bestSnapResult)
    {
        var snapThresholdOnCanvas = UserSettings.Config.SnapStrength / canvasScale;
        var distance = Math.Abs(anchorTime - targetTime);
            
        var force = Math.Max(0, Math.Abs(snapThresholdOnCanvas) - distance);
        if (force < 0.00001)
            return false;
            

        if (bestSnapResult != null && bestSnapResult.Force > force)
            return false;
            

        // Avoid allocation
        if (bestSnapResult == null)
        {
            bestSnapResult = new SnapResult(anchorTime, force);
        }
        else
        {
            bestSnapResult.Force = force;
            bestSnapResult.SnapToValue = anchorTime;
        }
        return true;
    }

    public static SnapResult FindSnapResult(double targetTime, IEnumerable<double> anchors, float canvasScale)
    {
        SnapResult bestMatch = null;
        foreach (var beatTime in anchors)
        {
            CheckForBetterSnapping(targetTime, beatTime, canvasScale, ref bestMatch);
        }
        return bestMatch;
    }
        
    public static SnapResult FindSnapResult(double targetTime, double anchor, float canvasScale)
    {
        SnapResult bestMatch = null;
        CheckForBetterSnapping(targetTime, anchor, canvasScale, ref bestMatch);
        return bestMatch;
    }

    public void DrawSnapIndicator(ICanvas canvas, Mode mode)
    {
        if (ImGui.GetTime() - _lastSnapTime > SnapIndicatorDuration)
            return;

        var opacity = (1 - ((float)(ImGui.GetTime() - _lastSnapTime) / SnapIndicatorDuration).Clamp(0, 1)) * 0.4f;
        var color = UiColors.StatusAnimated;
        color.Rgba.W = opacity;

        switch (mode)
        {
            case Mode.HorizontalLinesForV:
            {
                var p = new Vector2(0, canvas.TransformY((float)_lastSnapPosition));
                p.Y = (int)p.Y-1;
                ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(4000, 1), color);
                break;
            }
            case Mode.VerticalLinesForU:
            {
                var p = new Vector2(canvas.TransformX((float)_lastSnapPosition), 0);
                ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(1, 2000), color);
                break;
            }
        }
    }

    public enum Mode
    {
        HorizontalLinesForV,
        VerticalLinesForU,
    }
        
    private readonly List<IValueSnapAttractor> _snapAttractors = new();
    private double _lastSnapPosition;
    private double _lastSnapTime;
}