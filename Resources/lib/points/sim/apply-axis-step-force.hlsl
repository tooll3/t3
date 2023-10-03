#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float ApplyTrigger;
    float Strength;
    float RandomizeStrength;
    float SelectRatio;

    float3 AxisDistribution;
    float AddOriginalVelocity;

    float3 StrengthDistribution;
    float Seed;
}

RWStructuredBuffer<Particle> Particles : u0;

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);
    if(gi >= maxParticleCount) {
        return;
    }

    float4 random = hash41u(gi + (uint)Seed * 1103515245U);

    float selected = random.x < SelectRatio ? 1 : 0;
    float f =  selected* Strength *  (1+ RandomizeStrength * (random.z - 0.5));

    float3 axis = abs(random.zyx * AxisDistribution);
    float3 direction = axis.x > axis.y ? (axis.x > axis.z ? float3(1,0,0) : float3(0,0,1))
                                       : (axis.y > axis.z ? float3(0,1,0) : float3(0,0,1));
    
    direction *=  (random.w < 0.5 ? 1 : -1) * StrengthDistribution * f; 

    float3 origVelocity = Particles[gi].velocity;
    Particles[gi].velocity = lerp( origVelocity,
                                   origVelocity* AddOriginalVelocity + direction * f,
                                ApplyTrigger * selected);
}

