#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Frequency;
    float Phase;
    float Variation;
    float3 AmountDistribution;
    float UseCurlNoise;
}

RWStructuredBuffer<Point> Points : u0; 
RWStructuredBuffer<SimPoint> SimPoints : u1; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, _;
    Points.GetDimensions(pointCount, _);
    if(i.x >= pointCount) {
        return;
    }

    float3 variationOffset = hash41u(i.x).xyz * Variation;    
    float3 pos = Points[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    float3 noiseLookup = (pos + variationOffset + Phase* float3(1,-1,0)  ) * Frequency;

    SimPoints[i.x].Velocity += UseCurlNoise < 0.5 
        ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
        : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;


}

