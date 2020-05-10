using System;

namespace T3.Gui.ChildUi.Animators
{
    public struct SpeedRate
    {
        private SpeedRate(float f, string label)
        {
            Factor = f;
            Label = label;
        }

        public float Factor;
        public string Label;

        public static readonly SpeedRate[] RelevantRates =
            {
                new SpeedRate(-1, "Ignore"),
                new SpeedRate(0.0f, "OFF"),
                new SpeedRate(0.125f, "1/8"),
                new SpeedRate(0.25f, "1/4"),
                new SpeedRate(0.5f, "1/2"),
                new SpeedRate(1, "1"),
                new SpeedRate(4, "x4"),
                new SpeedRate(8, "x8"),
                new SpeedRate(16, "x16"),
                new SpeedRate(32, "x32"),
            };

        public static int FindCurrentRateIndex(float rate)
        {
            for (var index = 0; index < SpeedRate.RelevantRates.Length; index++)
            {
                if (Math.Abs(SpeedRate.RelevantRates[index].Factor - rate) < 0.01f)
                    return index;
            }

            return -1;
        }
    }
}