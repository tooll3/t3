#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Frequency;
    float Phase;
    float Variation;
    float3 AmountDistribution;
    float RotationLookupDistance;
    float UseCurlNoise;
}

// struct Point {
//     float3 Position;
//     float W;
// };

RWStructuredBuffer<Point> ResultPoints : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    ResultPoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        ResultPoints[i.x].w = 0 ;
        return;
    }

    float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    float3 pos = ResultPoints[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    float3 noiseLookup = (pos + variationOffset + Phase ) * Frequency;

    float3 noise = UseCurlNoise < 0.5 
        ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
        : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;

    float3 n = float3(1, 0.0, 0) * RotationLookupDistance;

    float3 posNormal = ResultPoints[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    float3 noiseLookupNormal = (posNormal + variationOffset + Phase  ) * Frequency + n/Frequency;
    float3 noiseNormal = UseCurlNoise < 0.5
        ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
        : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;
    float4 rotationFromDisplace = normalize(from_to_rotation(normalize(n), normalize(n+ noiseNormal) ) );

    ResultPoints[i.x].position += noise ;
    ResultPoints[i.x].rotation = qmul(rotationFromDisplace , ResultPoints[i.x].rotation);
}

