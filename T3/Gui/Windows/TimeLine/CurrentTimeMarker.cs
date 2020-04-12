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

            //var localTime = (playback.TimeInBars - TimeLineCanvas.Current._localOffset) / TimeLineCanvas.Current._localScale;
            //var p = new Vector2(TimeLineCanvas.Current.TransformPositionX((float)localTime), 0);
            var p = new Vector2(TimeLineCanvas.Current.TransformGlobalTime((float)playback.TimeInBars), 0);
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(1, 2000), Color.Orange);
        } 
        
        
        public SnapResult CheckForSnap(double time)
        {
            var timeX = TimeLineCanvas.Current.TransformU((float)time);
            var currentTime = TimeLineCanvas.Current.TransformU((float)_playback.TimeInBars);
            var distance = Math.Abs( timeX - currentTime);
            if (distance <= 0)
                return null;

            var force = Math.Max(0,SnapThreshold - distance); 
            return force >0 
                       ? new SnapResult(_playback.TimeInBars, force) 
                       : null;
        }
        
        private Playback _playback;
        private const double SnapThreshold = 8;
    }
}