using System.Collections.Generic;

namespace T3.Core.Animation;

public class LinearInterpolator
{
    public static void UpdateTangents(List<KeyValuePair<double, VDefinition>> curveElements) { }

    public static double Interpolate(KeyValuePair<double, VDefinition> a, KeyValuePair<double, VDefinition> b, double u)
    {
        return a.Value.Value + (b.Value.Value - a.Value.Value) * ((u - a.Key) / (b.Key - a.Key));
    }
};