using System.Collections.Generic;

namespace T3.Core.Animation.Curve
{
    public class ConstantCurveMapper : IOutsideCurveMapper
    {
        public void Calc(double u, SortedList<double, VDefinition> curveElements, out double newU, out double offset)
        {
            newU = u;
            offset = 0.0;
        }
    };
}