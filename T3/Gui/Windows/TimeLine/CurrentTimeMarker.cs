using System;
using System.Data.SqlTypes;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Animation.Snapping;
using T3.Operators.Types;
using UiHelpers;

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
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(1, 2000), Color.Red);
        } 
        
        
        public SnapResult CheckForSnap(double time)
        {
            var timeX = TimeLineCanvas.Current.TransformPositionX((float)time);
            var currentTime = TimeLineCanvas.Current.TransformPositionX((float)_clipTime.Time);
            var distance = timeX > currentTime
                               ? timeX - currentTime
                               : currentTime - timeX;
            
            return new SnapResult(_clipTime.Time, force:distance);
        }

        private ClipTime _clipTime;
        private const double SnapThreshold = 8;
    }
}