using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.TimeLine
{
    public class CurrentTimeMarker: IValueSnapAttractor
    {
        public void Draw(Playback playback)
        {
            if (playback == null)
                return;
            _playback = playback;

            var p = new Vector2(TimeLineCanvas.Current.TransformX((float)playback.TimeInBars), 0);
            var drawList = ImGui.GetWindowDrawList();
            var y = ImGui.GetWindowPos().Y;
            var windowHeight = ImGui.GetWindowHeight() +1;
            drawList.AddRectFilled(p + new Vector2(-1,y), p + new Vector2(2, windowHeight), UiColors.BackgroundFull.Fade(0.2f));
            drawList.AddRectFilled(p, p + new Vector2(1, y+ windowHeight), UiColors.StatusAnimated);
        }

        private static readonly Color _shadowColor = new(0, 0, 0, 0.4f);
        
        public SnapResult CheckForSnap(double time, float canvasScale)
        {
            return ValueSnapHandler.FindSnapResult(time, _playback.TimeInBars, canvasScale);
        }
        
        private Playback _playback;
        private const double SnapThreshold = 8;
    }
}