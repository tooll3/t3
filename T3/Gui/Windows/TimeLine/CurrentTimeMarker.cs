using System;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    public class CurrentTimeMarker: IValueSnapAttractor
    {
        public  void Draw(ClipTime clipTime)
        {
            if (clipTime == null)
                return;
            _clipTime = clipTime;

            var p = new Vector2(TimeLineCanvas.Current.TransformPositionX((float)clipTime.Time), 0);
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(1, 2000), Color.Orange);
        } 
        
        
        public SnapResult CheckForSnap(double time)
        {
            var timeX = TimeLineCanvas.Current.TransformPositionX((float)time);
            var currentTime = TimeLineCanvas.Current.TransformPositionX((float)_clipTime.Time);
            var distance = Math.Abs( timeX - currentTime);
            if (distance <= 0)
                return null;

            var force = Math.Max(0,SnapThreshold - distance); 
            return force >0 
                       ? new SnapResult(_clipTime.Time, force) 
                       : null;
        }
        
        private ClipTime _clipTime;
        private const double SnapThreshold = 8;
    }
}