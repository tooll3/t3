using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    public class CurrentTimeMarker: IValueSnapAttractor
    {
        public  void Draw(Playback playback)
        {
            if (playback == null)
                return;
            _playback = playback;

            var p = new Vector2(TimeLineCanvas.Current.TransformGlobalTime((float)playback.TimeInBars), 0);
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(1, 2000), Color.Orange);
        } 
        
        
        public SnapResult CheckForSnap(double time, float canvasScale)
        {
            return ValueSnapHandler.FindSnapResult(time, _playback.TimeInBars, canvasScale);
        }
        
        private Playback _playback;
        private const double SnapThreshold = 8;
    }
}