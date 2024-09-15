#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Frequency;
    float Phase;
    float Variation;
    float3 AmountDistribution;
    float UseCurlNoise;
    float AmountFromVelocity;
}

RWStructuredBuffer<Particle> Particles : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);
    if(i.x >= maxParticleCount) {
        return;
    }

    float3 variationOffset = hash41u(i.x).xyz * Variation;    
    float3 pos = Particles[i.x].Position*0.9; // avoid simplex noice glitch at -1,0,0 
    float3 noiseLookup = (pos + variationOffset + Phase* float3(1,-1,0)  ) * Frequency;
    float3 velocity = Particles[i.x].Velocity;
    float speed = length(velocity);
    float3 amount =  (Amount/100 + speed * AmountFromVelocity / 100 ) * AmountDistribution;

    Particles[i.x].Velocity = velocity + (UseCurlNoise < 0.5 
        ? snoiseVec3(noiseLookup)
        : curlNoise(noiseLookup)) * amount;
}

