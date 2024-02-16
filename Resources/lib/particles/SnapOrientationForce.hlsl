#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float SnapAngle;
    float PhaseAngle;
    float Variation;
    float VariationRatio;

    float KeepPlanar;
}

cbuffer Params : register(b1)
{
    int RandomSeed;
}

cbuffer Transforms : register(b2)
{
    float4x4 CameraToClipSpace;
    float4x4 ClipSpaceToCamera;
    float4x4 WorldToCamera;
    float4x4 CameraToWorld;
    float4x4 WorldToClipSpace;
    float4x4 ClipSpaceToWorld;
    float4x4 ObjectToWorld;
    float4x4 WorldToObject;
    float4x4 ObjectToCamera;
    float4x4 ObjectToClipSpace;
};


RWStructuredBuffer<Particle> Particles : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);

    int id = i.x;

    if(i.x >= maxParticleCount) {
        return;
    }

    float3 vInObject = Particles[i.x].Velocity;

    float4 vInCamera = mul(float4(vInObject, 0), WorldToCamera);
    float3 v = vInCamera;

    float lengthXY = length(v.xy);
    if(lengthXY < 0.00001)
        return;

    float2 normalizedV = normalize(v.xy);

    float a = atan2(normalizedV.x, normalizedV.y);

    float aNormalized = ((a + PI) / (PI*2)) %1;
    float subdivisions = 360 / SnapAngle;

    float4 hash = hash41u(id + RandomSeed * _PRIME0);
    if(hash.x < VariationRatio) {
        aNormalized += (hash.y - 0.5) * Variation ;
    }
    float t = aNormalized * subdivisions;

    float tRounded = ((int)(t + 0.5)) / subdivisions;
    
    float newAngle = lerp(aNormalized, tRounded, Amount);

    float alignedRotation = (newAngle - 0.5) * 2 * PI + (PhaseAngle/360);

    float2 newXY = float2(sin(alignedRotation), cos(alignedRotation)) * lengthXY;
    v.z *= (1-KeepPlanar);

    float3 newVInObject = mul( float4(newXY,v.z, 0),  CameraToWorld);
    Particles[i.x].Velocity = lerp(v, newVInObject, 1);
}

