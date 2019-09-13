using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;

namespace T3.Gui
{
    public class ClipTime
    {
        public double Time { get; set; } = 0;
        public double TimeRangeStart { get; set; } = 0;
        public double TimeRangeEnd { get; set; } = 8;
        public double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = true;

        public void Update()
        {
            Time += ImGui.GetIO().DeltaTime * PlaybackSpeed;
            if (IsLooping && Time > TimeRangeEnd)
            {
                Time = Time - TimeRangeEnd > 1  // JUmp to start if too far out of time region
                    ? TimeRangeStart
                    : Time - (TimeRangeEnd - TimeRangeStart);
            }
            EvaluationContext.GlobalTime = Time;
        }
    }
}
