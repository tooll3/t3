using System;
using System.Collections.Generic;
using T3.Core.Utils;

namespace T3.Core.Animation;

internal static class SplineInterpolator
{
    internal static void UpdateTangents(List<KeyValuePair<double, VDefinition>> points)
    {
        int count = points.Count;
        if (count <= 1)
            return;
        
        // Handle first point
        var first = points[0];
        var second = points[1];
        first.Value.OutTangentAngle = CalculateTangent(first, second, true);
        first.Value.InTangentAngle = first.Value.OutTangentAngle - Math.PI;

        // Handle middle points
        for (int i = 1; i < count - 1; i++)
        {
            var prev = points[i - 1];
            var current = points[i];
            var next = points[i + 1];

            current.Value.InTangentAngle = CalculateInTangent(prev, current, next);
            current.Value.OutTangentAngle = CalculateOutTangent(prev, current, next);
        }

        // Handle last point
        var last = points[count - 1];
        var secondLast = points[count - 2];
        last.Value.InTangentAngle = CalculateTangent(secondLast, last, false);
        last.Value.OutTangentAngle = last.Value.InTangentAngle - Math.PI;
    }

    private const float NormalWeight = 1/3f;
    
    internal static double Interpolate(KeyValuePair<double, VDefinition> a, KeyValuePair<double, VDefinition> b, double u)
    {
        var weightA = NormalWeight;
        var weightB = NormalWeight;

        var needIterativeSampling = Math.Abs(weightA - NormalWeight) > 0.01f 
                                    || Math.Abs(weightB - NormalWeight) > 0.01f;

        if (needIterativeSampling)
        {
            return SampleWeightedCurve(
                                             a.Key, 
                                             a.Value.Value, 
                                             Math.Tan(a.Value.OutTangentAngle),
                                             weightA, 
                                             b.Key, 
                                             b.Value.Value, 
                                             Math.Tan(b.Value.OutTangentAngle),
                                             weightA, 
                                             u);
        }

        var t = (u - a.Key) / (b.Key - a.Key);

        t = ((float)t).ApplyGainAndBias(0.3f, 0.5f);
        var baseLength = (b.Key - a.Key);
        
        // 4) Compute tangents
        var m0 = Math.Tan(a.Value.OutTangentAngle) * baseLength;
        var m1 = Math.Tan(b.Value.InTangentAngle)  * baseLength;

        // 5) Hermite interpolation
        var t2 = t * t;
        var t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * a.Value.Value
               + (t3 - 2 * t2 + t)  * m0
               + (-2 * t3 + 3 * t2) * b.Value.Value
               + (t3 - t2)         * m1;    
    }

    public const double Eps = 2.22e-16;

    // Also see: https://math.stackexchange.com/questions/3210725/weighting-a-cubic-hermite-spline
    public static double SampleWeightedCurve(double x1, double y1, double yp1, double wt1, double x2, double y2, double yp2, double wt2, double x)
    {
        double dx = x2 - x1;
        x = (x - x1) / dx;
        double dy = y2 - y1;
        yp1 = yp1 * dx / dy;
        yp2 = yp2 * dx / dy;
        double wt2s = 1 - wt2;

        double t = 0.5;
        double t2 = 0;

        var count = 0;
        if (wt1 == 1 / 3.0 && wt2 == 1 / 3.0)
        {
            t  = x;
            t2 = 1 - t;
        }
        else
        {
            while (count++ < 10)
            {
                t2 = (1 - t);
                double fg = 3 * t2 * t2 * t * wt1 + 3 * t2 * t * t * wt2s + t * t * t - x;
                if (Math.Abs(fg) < 2*Eps)
                    break;

                // third order householder method
                double fpg = 3 * t2 * t2 * wt1 + 6 * t2 * t * (wt2s - wt1) + 3 * t * t * (1 - wt2s);
                double fppg = 6 * t2 * (wt2s - 2 * wt1) + 6 * t * (1 - 2 * wt2s + wt1);
                double fpppg = 18 * wt1 - 18 * wt2s + 6;
                
                t -= (6 * fg * fpg * fpg - 3 * fg * fg * fppg) / (6 * fpg * fpg * fpg - 6 * fg * fpg * fppg + fg * fg * fpppg);
            }
        }
        
        double y = 3 * t2 * t2 * t * wt1 * yp1 + 3 * t2 * t * t * (1 - wt2 * yp2) + t * t * t;
        
        
        return y * dy + y1;
    }    
    
    
    // Unified method for start/end tangent calculation
    private static double CalculateTangent(KeyValuePair<double, VDefinition> a, KeyValuePair<double, VDefinition> b, bool isStart)
    {
        var editMode = isStart ? a.Value.OutEditMode : b.Value.InEditMode;
        
        switch (editMode)
        {
            case VDefinition.EditMode.Tangent:
                return isStart ? a.Value.OutTangentAngle : b.Value.InTangentAngle;
                
            case VDefinition.EditMode.Linear:
                return Math.PI / 2 - Math.Atan2(
                    isStart ? a.Key - b.Key : b.Key - a.Key,
                    isStart ? a.Value.Value - b.Value.Value : b.Value.Value - a.Value.Value);
                
            default:
                return isStart ? Math.PI : 0;
        }
    }

    private static double CalculateInTangent(KeyValuePair<double, VDefinition> prev, KeyValuePair<double, VDefinition> cur, KeyValuePair<double, VDefinition> next)
    {
        switch (cur.Value.InEditMode)
        {
            case VDefinition.EditMode.Tangent:
                return cur.Value.InTangentAngle;
                
            case VDefinition.EditMode.Linear:
                return Math.PI / 2 - Math.Atan2(cur.Key - prev.Key, cur.Value.Value - prev.Value.Value);
                
            case VDefinition.EditMode.Cubic:
                return Math.PI / 2 - Math.Atan2(next.Key - prev.Key, next.Value.Value - prev.Value.Value);
                
            case VDefinition.EditMode.Smooth:
                return CalculateSmoothTangent(prev, cur, next, true);
                
            case VDefinition.EditMode.Horizontal:
            default:
                return 0;
        }
    }

    private static double CalculateOutTangent(KeyValuePair<double, VDefinition> prev, KeyValuePair<double, VDefinition> cur, KeyValuePair<double, VDefinition> next)
    {
        switch (cur.Value.OutEditMode)
        {
            case VDefinition.EditMode.Tangent:
                return cur.Value.OutTangentAngle;
                
            case VDefinition.EditMode.Linear:
                return Math.PI / 2 - Math.Atan2(cur.Key - next.Key, cur.Value.Value - next.Value.Value);
                
            case VDefinition.EditMode.Cubic:
                return Math.PI / 2 - Math.Atan2(prev.Key - next.Key, prev.Value.Value - next.Value.Value);
                
            case VDefinition.EditMode.Smooth:
                return CalculateSmoothTangent(prev, cur, next, false);
                
            case VDefinition.EditMode.Horizontal:
            default:
                return Math.PI;
        }
    }

    private static double CalculateSmoothTangent(KeyValuePair<double, VDefinition> prev, KeyValuePair<double, VDefinition> cur, KeyValuePair<double, VDefinition> next, bool isIn)
    {
        double thirdToPrev = (prev.Key - cur.Key) / 3f;
        double thirdToNext = (next.Key - cur.Key) / 3f;
        
        // Base angle calculation - reversed for in vs out tangent
        double angle = Math.PI / 2 - Math.Atan2(
            isIn ? next.Key - prev.Key : prev.Key - next.Key,
            isIn ? next.Value.Value - prev.Value.Value : prev.Value.Value - next.Value.Value);

        // Adjust for potential overshooting
        bool isAscending = prev.Value.Value < next.Value.Value;
        
        // Check overshooting to next point
        if (WouldOvershoot(cur, next, angle, thirdToNext, isAscending))
        {
            angle = CalculateAngleToAvoidOvershoot(cur, next, thirdToNext, isAscending, isIn);
        }
        // Check overshooting to previous point
        else if (WouldOvershoot(cur, prev, angle, thirdToPrev, !isAscending))
        {
            angle = Math.PI + CalculateAngleToAvoidOvershoot(cur, prev, thirdToPrev, !isAscending, isIn);
        }
        
        return angle;
    }
    
    private static bool WouldOvershoot(KeyValuePair<double, VDefinition> cur, KeyValuePair<double, VDefinition> target, 
                                      double angle, double distance, bool isAscending)
    {
        double projectedValue = cur.Value.Value + Math.Tan(angle) * distance;
        return (isAscending && projectedValue > target.Value.Value) || 
               (!isAscending && projectedValue < target.Value.Value);
    }
    
    private static double CalculateAngleToAvoidOvershoot(KeyValuePair<double, VDefinition> cur, KeyValuePair<double, VDefinition> target,
                                                       double distance, bool isAscending, bool isIn)
    {
        double valueDiff = isAscending ? 
            Math.Max(0, -cur.Value.Value + target.Value.Value) : 
            Math.Min(0, -cur.Value.Value + target.Value.Value);
            
        double xDir = isIn ? distance : -distance;
        return isIn ?  0.5f* Math.PI - Math.Atan2(xDir, valueDiff)
                       : 1.5f * Math.PI + Math.Atan2(xDir, valueDiff);
    }
}