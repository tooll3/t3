#include "lib/shared/particle.hlsl"
#include "lib/shared/noise-functions.hlsl"

cbuffer TimeConstants : register(b0)
{
    float GlobalTime;
    float Time;
    float RunTime;
    float BeatTime;
    float LastFrameDuration;
}

cbuffer Transforms : register(b1)
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
RWStructuredBuffer<ParticleIndex> AliveParticles : u1;
AppendStructuredBuffer<ParticleIndex> DeadParticles : u2;
RWBuffer<uint> IndirectArgs : u3;

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    // Initialize the draw args using the first thread in the Dispatch call
    if (i.x == 0)
    {
        IndirectArgs[0] = 0;	// Number of primitives reset to zero
        IndirectArgs[1] = 1;	// Number of instances is always 1
        IndirectArgs[2] = 0;
        IndirectArgs[3] = 0;
    }

    // Wait after draw args are written so no other threads can write to them before they are initialized
    GroupMemoryBarrierWithGroupSync();

    float oldLifetime = Particles[i.x].lifetime;
    float newLifetime = oldLifetime - LastFrameDuration;

    if (newLifetime < 0.0)
    {
        if (oldLifetime >= 0.0)
        {
            ParticleIndex pi;
            pi.index = i.x;
            pi.squaredDistToCamera = 99999.0;
            DeadParticles.Append(pi);
        }
    }
    else
    {
        uint index = AliveParticles.IncrementCounter();
        AliveParticles[index].index = i.x;
        // float3 camPosInWorld = CameraToWorld[3].xyz;
        // float3 attractorInWorld = camPosInWorld + float3(0, -1, 4);
        float3 posInCamera = mul(Particles[i.x].position, ObjectToCamera).xyz; // todo: optimize
        AliveParticles[index].squaredDistToCamera = posInCamera.z;//dot(-WorldToCamera[2].xyz, posInCamera);
        /*
        float3 attractorInWorld = float3(cos(BeatTime)*20.0, 0, sin(BeatTime)*20.0);
        float3 dir = attractorInWorld - Particles[i.x].position;
        float distToAttractor = dot(dir, dir);
        float3 Fa = (distToAttractor < 110.0) ? dir*1150.0 / distToAttractor : float3(0,0,0);

        float3 Fg = float3(0, -10, 0);
        //float3 Fc = curlNoise(Particles[i.x].position*0.14)*10.0;
        //Fc *= 0.0;
        float3 Fc = float3(0,0,0);
        float3 F = Fc + Fa + Fg;
        float3 a = F / Particles[i.x].mass;
        float delta = LastFrameDuration;
        float3 v = a*delta;
        float3 s = v*delta;
        Particles[i.x].velocity = v;
        Particles[i.x].position += s;
        */
        uint originalValue;
        InterlockedAdd(IndirectArgs[0], 6, originalValue);
    }

    Particles[i.x].lifetime = newLifetime;
}

