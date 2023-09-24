#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformVolume;

    float FallOff;
    float Bias;
    float Strength;
    float DistanceMode;
}

StructuredBuffer<Point> TargetPoints : t0;   

RWStructuredBuffer<Point> Points : u0; 
RWStructuredBuffer<SimPoint> SimPoints : u1; 

static const int VolumeSphere = 0;
static const int VolumeBox = 1;
static const int VolumePlane = 2;
static const int VolumeZebra = 3;
static const int VolumeNoise = 4;

inline float Bias2(float x, float bias)
{
    return bias < 0
               ? pow(x, clamp(bias + 1, 0.005, 1))
               : 1 - pow(1 - x, clamp(1 - bias, 0.005, 1));
}

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint targetPointCount, simPointCount, stride;
    Points.GetDimensions(simPointCount, stride);
    if(i.x >= simPointCount) 
        return;
    
    TargetPoints.GetDimensions(targetPointCount, stride);
    uint targetPointIndex= i.x % targetPointCount;

    float3 pos = Points[i.x].position;
    float4 rot = Points[i.x].rotation;
    float3 velocity = SimPoints[i.x].Velocity;

    float3 usedPos = DistanceMode < 0.5 ? pos : TargetPoints[i.x].position;
    float3 posInVolume = mul(float4(usedPos, 1), TransformVolume).xyz;
    float d =length(posInVolume); 
    const float r= 0.5;
    float t= (d + r*(FallOff -1)) / (2 * r * FallOff);
    float blendFactor = smoothstep(1,0, t) * Strength; 

    Points[i.x].w = blendFactor;
    Points[i.x].position = lerp(pos, TargetPoints[targetPointIndex].position, blendFactor);
    Points[i.x].rotation = q_slerp(rot, TargetPoints[targetPointIndex].rotation, blendFactor);
    SimPoints[i.x].Velocity = lerp(velocity, 0, blendFactor);    
}

