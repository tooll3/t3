#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float BlendFactor;
    float BlendMode;
    float PairingMode;
    float Width;
    float Scatter;
}

StructuredBuffer<Point> PointsA : t0;        // input
StructuredBuffer<Point> PointsB : t1;        // input
RWStructuredBuffer<Point> ResultPoints : u0; // output

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint resultCount, countA, countB, stride;
    ResultPoints.GetDimensions(resultCount, stride);
    PointsA.GetDimensions(countA, stride);
    PointsB.GetDimensions(countB, stride);

    if (i.x > resultCount)
        return;

    uint aIndex = i.x;
    uint bIndex = i.x;

    float t = i.x / (float)resultCount;

    if (PairingMode > 0.5 && countA != countB)
    {
        aIndex = (int)(countA * t);
        bIndex = (int)(countB * t);
    }

    Point A = PointsA[aIndex];
    Point B = PointsB[bIndex];

    float f = 0;

    if (BlendMode < 0.5)
    {
        f = BlendFactor;
    }
    else if (BlendMode < 1.5)
    {
        f = A.W;
    }
    else if (BlendMode < 2.5)
    {
        f = (1 - B.W);
    }

    // Ranged
    // see https://www.desmos.com/calculator/zxs1fy06uh
    else if (BlendMode < 3.5)
    {
        f = 1 - saturate((t - BlendFactor) / Width - BlendFactor + 1);
    }
    else
    {
        float b = BlendFactor % 2;
        if (b > 1)
        {
            b = 2 - b;
            t = 1 - t;
        }
        f = 1 - smoothstep(0, 1, saturate((t - b) / Width - b + 1));
    }

    float fallOffFromCenter = smoothstep(0, 1, 1 - abs(f - 0.5) * 2);
    f += (hash11(t) - 0.5) * Scatter * fallOffFromCenter;

    ResultPoints[i.x].Rotation = qSlerp(A.Rotation, B.Rotation, f);
    ResultPoints[i.x].Position = lerp(A.Position, B.Position, f);
    ResultPoints[i.x].W = lerp(A.W, B.W, f);
    ResultPoints[i.x].Color = lerp(A.Color, B.Color, f);
    ResultPoints[i.x].Stretch = lerp(A.Stretch, B.Stretch, f);
    ResultPoints[i.x].Selected = lerp(A.Selected, B.Selected, f);
}
