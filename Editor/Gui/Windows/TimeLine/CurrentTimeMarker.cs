using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.TimeLine;

internal class CurrentTimeMarker: IValueSnapAttractor
{
    public void Draw(double timeInBars, TimeLineCanvas timelineCanvas)
    {
        _currentTimeInBars = timeInBars;

        var p = new Vector2(timelineCanvas.TransformX((float)timeInBars), 0);
        var drawList = ImGui.GetWindowDrawList();
        var y = ImGui.GetWindowPos().Y;
        var windowHeight = ImGui.GetWindowHeight() +1;
        drawList.AddRectFilled(p + new Vector2(-1,y), p + new Vector2(2, windowHeight), UiColors.BackgroundFull.Fade(0.2f));
        drawList.AddRectFilled(p, p + new Vector2(1, y+ windowHeight), UiColors.StatusAnimated);
    }

    private static readonly Color _shadowColor = new(0, 0, 0, 0.4f);
        
    public SnapResult CheckForSnap(double time, float canvasScale)
    {
        return ValueSnapHandler.FindSnapResult(time, _currentTimeInBars, canvasScale);
    }
        
    private double _currentTimeInBars;
    private const double SnapThreshold = 8;
}