#include "hash-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float BlendFactor;
    float PairingMethod;
    float CountA;
    float CountB;
}

// struct Point
// {
//     float3 position;
//     float w;
//     float4 rotation;
// };

StructuredBuffer<Point> PointsB : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    //float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    float3 posA = ResultPoints[i.x].position;
    float3 posB = PointsB[i.x].position;
    float wA = ResultPoints[i.x].w;
    float wB = ResultPoints[i.x].w;

    ResultPoints[i.x].position = lerp(posA, posB, BlendFactor);
    ResultPoints[i.x].w = lerp(wA, wB, BlendFactor); ;
}

