using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Gui
{
    public class ClipTime
    {
        public double Time { get; set; } = 0;
        public double TimeRangeStart { get; set; } = 5;
        public double TimeRangeEnd { get; set; } = 30;
        public double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = true;

        public void Update()
        {
            Time += ImGui.GetIO().DeltaTime * PlaybackSpeed;
            if (IsLooping && Time > TimeRangeEnd)
            {
                Time = Time - TimeRangeEnd > 1
                    ? TimeRangeStart
                    : Time - TimeRangeEnd - TimeRangeStart;
            }
        }
    }

}
