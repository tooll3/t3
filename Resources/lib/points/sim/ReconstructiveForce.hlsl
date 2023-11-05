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
RWStructuredBuffer<Particle> Particles : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    uint targetPointCount, maxParticleCount, stride;
    Particles.GetDimensions(maxParticleCount, stride);
    if(gi >= maxParticleCount) 
        return;

    if(isnan(TransformVolume._11) || TransformVolume._11 == 0)
       return;

    TargetPoints.GetDimensions(targetPointCount, stride);
    uint targetPointIndex= gi % targetPointCount;

    float3 pos = Particles[gi].p.position;
    float4 rot = Particles[gi].p.rotation;
    float3 velocity = Particles[gi].velocity;

    float3 usedPos = DistanceMode < 0.5 ? pos : TargetPoints[gi].position;
    float3 posInVolume = mul(float4(usedPos, 1), TransformVolume).xyz;
    float d =length(posInVolume); 
    const float r= 0.5;
    float t= (d + r*(FallOff -1)) / (2 * r * FallOff);
    float blendFactor = smoothstep(1,0, t) * Strength; 

    //Particles[gi].p.w = blendFactor;
    Particles[gi].p.position = lerp(pos, TargetPoints[targetPointIndex].position, blendFactor);
    Particles[gi].p.rotation = q_slerp(rot, TargetPoints[targetPointIndex].rotation, blendFactor);
    Particles[gi].velocity = lerp(velocity, 0, blendFactor);    
}

