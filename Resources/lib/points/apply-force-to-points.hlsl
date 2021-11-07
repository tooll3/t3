#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float3 Center;
    float Radius;
    float Force;
}

RWStructuredBuffer<Point> Points : u0; 


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    Points.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        Points[i.x].w = 0 ;
        return;
    }

    float3 pos = Points[i.x].position;


    float3 direction = pos-Center;
    float3 v = Points[i.x].w * rotate_vector(float3(0,0,1), Points[i.x].rotation);

    float distanceToCenter = length(direction);

    float f= 1- saturate( distanceToCenter / Radius);
    if(f < 0.001)
        return;

    
    float3 newV = v + (direction/ distanceToCenter) * Force * f;
    float4 newOrientation = q_look_at( normalize(newV), float3(0,1,0));
    Points[i.x].w = length(newV);

    //float4 newOrientation = q_look_at( normalize(direction), float3(0,1,0));
    Points[i.x].rotation = newOrientation;

    // float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    // float3 pos = Points[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    // float3 noiseLookup = (pos + variationOffset + Phase ) * Frequency;

    // float3 noise = UseCurlNoise < 0.5 
    //     ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
    //     : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;

    // float3 n = float3(1, 0.0, 0) * RotationLookupDistance;

    // float3 posNormal = Points[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    // float3 noiseLookupNormal = (posNormal + variationOffset + Phase  ) * Frequency + n/Frequency;
    // float3 noiseNormal = UseCurlNoise < 0.5
    //     ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
    //     : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;
    // float4 rotationFromDisplace = normalize(from_to_rotation(normalize(n), normalize(n+ noiseNormal) ) );

    // Points[i.x].position += noise ;
    //Points[i.x].rotation = qmul(rotationFromDisplace , Points[i.x].rotation);
}

