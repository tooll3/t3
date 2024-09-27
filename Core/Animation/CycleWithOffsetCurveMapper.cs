using System.Collections.Generic;
using System.Linq;

namespace T3.Core.Animation;

public class CycleWithOffsetCurveMapper : IOutsideCurveMapper
{
    public void Calc(double u, SortedList<double, VDefinition> curveElements, out double newU, out double offset)
    {
        offset = 0.0;
        if (curveElements.Count < 2)
        {
            newU = u;
        }
        else
        {
            var first = curveElements.First();
            var last = curveElements.Last();
            double firstU = first.Key;
            double lastU = last.Key;
            double delta = 0.0;
            double off = last.Value.Value - first.Value.Value;

            if (u < firstU)
            {
                delta = firstU - u;
                newU = lastU - (delta % (lastU - firstU));
                offset = off * (-((int)(delta / (lastU - firstU)) + 1));
            }
            else if (u > lastU)
            {
                delta = u - lastU;
                newU = firstU + (delta % (lastU - firstU));
                offset = off * ((int)(delta / (lastU - firstU)) + 1);
            }
            else
            {
                newU = u;
            }
        }
    }
};