#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"


cbuffer Params : register(b0)
{
    float3 RandomizePosition;
    float Amount;

    float3 RandomizeRotation;
    float RandomizeW;
    
    float UseLocalSpace;
    float Seed;

    float Bias;
    float Offset;

    float UseWAsSelection;
}

StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

float4 GetBias(float4 x, float bias)
{
    return x / ((1 / bias - 2) * (1 - x) + 1);
}

float4 GetGain(float4 x, float gain)
{
    return x < 0.5 ? GetBias(x * 2.0, gain)/2.0
                    : GetBias(x * 2.0 - 1.0,1.0 - gain)/2.0 + 0.5;
}


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    // if(i.x >= pointCount) {
    //     ResultPoints[i.x].w = 0 ;
    //     return;
    // }

    int pointId = i.x;
    float f = pointId / (float)pointCount;
    float phase = Seed + 133.1123 * f;
    int phaseId = (int)phase; 
    float clampedBias = clamp(Bias, 0.001, 0.999);
    float4 normalizedScatter = lerp(hash41u(pointId * 12341 + phaseId),
                                    hash41u(pointId * 12341 + phaseId + 1),
                                    smoothstep(0, 1,
                                               phase - phaseId));



    float4 hashRot = lerp(hash41u(pointId * 2723 + phaseId),
                                    hash41u(pointId * 2723 + phaseId + 1),
                                    smoothstep(0, 1,
                                               phase - phaseId)) * 2 - 1;

    //float rand = (i.x + 0.5) * 1.431 + 111 + floor(Seed+0.5) * 37.1;
    //float4 hash4 = hash42(rand);
    float4 hash4 =  GetGain(normalizedScatter, clampedBias) * 2 -1;
    
    //float4 hashRot = hash42( float2(rand, 23.1));

    float4 rot = SourcePoints[i.x].rotation;

    float amount = Amount * (UseWAsSelection > 0.5 ? SourcePoints[i.x].w : 1);
 
    float3 offset = hash4.xyz * RandomizePosition * amount;

    if(UseLocalSpace < 0.5)
    {
        offset = rotate_vector2(offset, rot);
    }

    ResultPoints[i.x].position = SourcePoints[i.x].position + offset;

    float3 randomRotate = (hashRot.xyz - 0.5) * (RandomizeRotation / 180 * PI) * amount * clampedBias;

    rot = normalize(qmul(rot, rotate_angle_axis(randomRotate.x * Offset, float3(1,0,0))));
    rot = normalize(qmul(rot, rotate_angle_axis(randomRotate.y * Offset, float3(0,1,0))));
    rot = normalize(qmul(rot, rotate_angle_axis(randomRotate.z * Offset, float3(0,0,1))));

    ResultPoints[i.x].rotation = rot;

    ResultPoints[i.x].w =  SourcePoints[i.x].w + hash4.w *RandomizeW * amount;
}

