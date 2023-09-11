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
        return;
    }

    //float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    float3 variationOffset = hash41u(i.x).xyz * Variation;    

    float3 pos = ResultPoints[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    float3 noiseLookup = (pos + variationOffset + Phase* float3(1,-1,0)  ) * Frequency;

    float3 noise = UseCurlNoise < 0.5 
        ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
        : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;

    float4 rot = ResultPoints[i.x].rotation;
    float4 normalizedRot;
    float v = q_separate_v(rot, normalizedRot);

    float3 forward = rotate_vector(float3(0,0,1), normalizedRot) * v;    
    forward += noise;

    float newV = length(forward);
    float4 newRotation = q_look_at(normalize(forward), float3(0,0,1));

    ResultPoints[i.x].rotation = q_encode_v(newRotation, newV);   


    // float3 n = float3(1, 0.0, 0) * RotationLookupDistance;

    // float3 posNormal = ResultPoints[i.x].position*0.9; // avoid simplex noice glitch at -1,0,0 
    // float3 noiseLookupNormal = (posNormal + variationOffset + Phase * float3(0,-1,0)  ) * Frequency + n/Frequency;
    // float3 noiseNormal = UseCurlNoise < 0.5
    //     ? snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution
    //     : curlNoise(noiseLookup) * Amount/100 * AmountDistribution;
    // float4 rotationFromDisplace = normalize(from_to_rotation(normalize(n), normalize(n+ noiseNormal) ) );

    // ResultPoints[i.x].position += noise ;
    // ResultPoints[i.x].rotation = qmul(rotationFromDisplace , ResultPoints[i.x].rotation);
}

