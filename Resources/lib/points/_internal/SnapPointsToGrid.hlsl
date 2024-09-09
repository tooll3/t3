#include "lib/shared/hash-functions.hlsl"
//  #include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"

cbuffer Params : register(b0)
{
    float3 GridStretch;
    float Amount;

    float3 GridOffset;
    float GridScale;

    float Scatter;
    float Mode;
    float2 BiasAndGain;

    float UseWAsWeight;
    float UseSelection;
}

StructuredBuffer<Point> Points1 : t0;
RWStructuredBuffer<Point> ResultPoints : u0;

[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    Point p = Points1[i.x];

    float3 gridSize = GridScale * GridStretch;
    float3 orgPosition = p.Position;

    float3 pos = orgPosition;
    float3 normalizedPosition = pos / gridSize;
    float3 normlizedOffsetPosition = normalizedPosition + 0.5 - GridOffset;
    float3 signedFraction = (mod(normlizedOffsetPosition, 1) - 0.5) * 2;
    float3 centerPoint = pos - signedFraction * gridSize / 2;

    float wFactor = UseWAsWeight > 0.5 ? p.W : 1;
    float selectionFactor = UseSelection > 0.5 ? p.Selected : 1;

    float3 scatter = (hash41u(i.x) - 0.5) * Scatter;

    float3 snapAmount = 0;
    if (Mode < 0.5)
    {
        snapAmount = saturate((length(signedFraction * gridSize) / length(gridSize) + scatter.x));
    }
    else if (Mode < 1.5)
    {
        snapAmount = 1 - saturate((length(signedFraction * gridSize) / length(gridSize) + scatter.x));
    }
    else if (Mode < 2.5)
    {
        snapAmount = abs(signedFraction + scatter);
    }
    else if (Mode < 3.5)
    {
        snapAmount = 1 - abs(signedFraction + scatter);
    }

    float3 biasedSnap = ApplyBiasAndGain(snapAmount.xyzz, BiasAndGain.x, BiasAndGain.y).xyz;

    float3 ff = (1 - saturate(biasedSnap - Amount * 2 + 1)) * selectionFactor * wFactor;
    p.Position = lerp(orgPosition, centerPoint, ff);
    ResultPoints[i.x] = p;
}
