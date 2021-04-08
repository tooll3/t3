using System.Collections.Generic;
using T3.Core.Animation;

namespace T3.Gui.Windows.TimeLine
{
    public class AnimationOperations
    {
        public static List<VDefinition> InsertKeyframeToCurves(IEnumerable<Curve> curves, double time, float increment = 0)
        {
            var newKeyframes = new List<VDefinition>(4);
            foreach (var curve in curves)
            {
                var value = curve.GetSampledValue(time);
                var previousU = curve.GetPreviousU(time);

                var key = (previousU != null)
                              ? curve.GetV(previousU.Value).Clone()
                              : new VDefinition();

                key.Value = value + increment;
                key.U = time;

                var newKey = key;
                curve.AddOrUpdateV(time, key);
                newKeyframes.Add(newKey);
            }
            return newKeyframes;
        }

        public static void RemoveKeyframeFromCurves(IEnumerable<Curve> curves, double time)
        {
            foreach (var curve in curves)
            {
                if (curve.HasVAt(time))
                {
                    curve.RemoveKeyframeAt(time);
                }
            }
        }
    }
}