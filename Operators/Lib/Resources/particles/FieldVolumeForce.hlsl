#include "shared/hash-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/pbr.hlsl"

/*{ADDITIONAL_INCLUDES}*/

cbuffer Params : register(b0)
{
    float Amount;
    float Attraction;
    float AttractionDecay;
    float Repulsion;

    float Bounciness;
    float RandomizeBounce;
    float RandomizeReflection;
    float InvertVolumeFactor;

    float NormalSamplingDistance;
    float SpeedFactor;
}

cbuffer Params : register(b1)
{
    /*{FLOAT_PARAMS}*/
}

cbuffer Params : register(b2)
{
    uint ParticleCount;
}

RWStructuredBuffer<Particle> Particles : u0;
StructuredBuffer<PbrVertex> Vertices : t0;
StructuredBuffer<int3> Indices : t1;
/*{RESOURCES}*/

//=== Field functions ===============================================
/*{FIELD_FUNCTIONS}*/

//-------------------------------------------------------------------
float4 GetField(float4 p)
{
    float4 f = 1;
    /*{FIELD_CALL}*/
    return f;
}

inline float GetDistance(float3 p3)
{
    return GetField(float4(p3.xyz, 0)).w;
}

//===================================================================

float3 GetNormal(float3 p, float offset)
{
    return normalize(
        GetDistance(p + float3(NormalSamplingDistance, -NormalSamplingDistance, -NormalSamplingDistance)) * float3(1, -1, -1) +
        GetDistance(p + float3(-NormalSamplingDistance, NormalSamplingDistance, -NormalSamplingDistance)) * float3(-1, 1, -1) +
        GetDistance(p + float3(-NormalSamplingDistance, -NormalSamplingDistance, NormalSamplingDistance)) * float3(-1, -1, 1) +
        GetDistance(p + float3(NormalSamplingDistance, NormalSamplingDistance, NormalSamplingDistance)) * float3(1, 1, 1));
}

float4 q_from_tangentAndNormal(float3 dx, float3 dz)
{
    dx = normalize(dx);
    dz = normalize(dz);
    float3 dy = -cross(dx, dz);

    float3x3 orientationDest = float3x3(
        dx,
        dy,
        dz);

    return normalize(qFromMatrix3Precise(transpose(orientationDest)));
}

[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);
    int gi = i.x;
    if (gi >= maxParticleCount)
        return;

    if (isnan(Particles[gi].BirthTime))
        return;

    // return;
    // if (isnan(TransformVolume._11) || TransformVolume._11 == 0)
    // {
    //     return;
    // }

    float3 pos = Particles[gi].Position;
    float4 rot = Particles[gi].Rotation;
    float3 velocity = Particles[gi].Velocity;
    float r = Particles[gi].Radius;

    float3 posNext = float4(pos + velocity * SpeedFactor * 0.01 * 2, 1);

    float rUnitLength = r / 2;
    // float3 rInVolume = length(mul(float4(unitLength.xxx, 0), TransformVolume));

    // float s = 1;
    // float distance = 0;
    // float distanceNext = 0;

    // float rUnitSphere = 0.5 * ;
    float distance = GetDistance(pos);
    float distanceNext = GetDistance(posNext);
    float3 surfaceN = GetNormal(pos, NormalSamplingDistance);
    // s = smoothstep(1 + FallOff, 1, distance);

    // if (VolumeShape == VolumeSphere)
    // {
    //     float rUnitSphere = 0.5;
    //     distance = length(posInVolume) - rUnitSphere;
    //     distanceNext = length(posInVolumeNext) - rUnitSphere;
    //     surfaceN = normalize(posInVolume);
    // // s = smoothstep(1 + FallOff, 1, distance);
    // }
    // else if (VolumeShape == VolumeBox)
    // {
    //     float3 t1 = abs(posInVolume);
    //     surfaceN = t1.x > t1.y ? (t1.x > t1.z ? float3(sign(posInVolume.x), 0, 0) : float3(0, 0, sign(posInVolume.z)))
    //                            : (t1.y > t1.z ? float3(0, sign(posInVolume.y), 0) : float3(0, 0, sign(posInVolume.z)));

    //     float r1 = length(abs(rInVolume * surfaceN)) * InvertVolumeFactor;

    //     float rUnitSphere = 0.5;
    //     distance = max(max(t1.x, t1.y), t1.z) - rUnitSphere - r1;

    //     float3 t2 = abs(posInVolumeNext);
    //     distanceNext = max(max(t2.x, t2.y), t2.z) - rUnitSphere - r1;
    // }
    // else if (VolumeShape == VolumePlane)
    // {
    //     distance = posInVolume.y - r * InvertVolumeFactor;
    //     distanceNext = posInVolumeNext.y - r * InvertVolumeFactor;
    //     surfaceN = float3(0, 1, 0);
    //     // s = smoothstep(FallOff, 0, distance);
    // }
    // else if (VolumeShape == VolumeCylinder)
    // {
    //     // Assuming the cylinder is aligned along the y-axis
    //     float rCylinder = 0.5;
    //     float heightCylinder = 1.0;

    //     float2 xyPos = posInVolume.xz;
    //     float2 xyPosNext = posInVolumeNext.xz;

    //     float distanceToCenter = length(xyPos);
    //     float distanceToCenterNext = length(xyPosNext);

    //     // Check if the particle is within the radius of the cylinder
    //     if (distanceToCenter <= rCylinder)
    //     {
    //         distance = abs(posInVolume.y) - heightCylinder * 0.5;
    //         distanceNext = abs(posInVolumeNext.y) - heightCylinder * 0.5;

    //         // Set the surface normal based on the cylinder's orientation
    //         surfaceN = float3(0, sign(posInVolume.y), 0);
    //     }
    //     else
    //     {
    //         // Particle is outside the cylinder, use the distance to the cylinder surface
    //         distance = distanceToCenter - rCylinder;
    //         distanceNext = distanceToCenterNext - rCylinder;

    //         // Set the surface normal based on the cylinder's orientation
    //         surfaceN = float3(xyPos.x, 0, xyPos.y);
    //         surfaceN.y = 0; // Ignore the y-component, as it's already handled above
    //         surfaceN = normalize(surfaceN);
    //     }
    // }

    float3 force = 0;

    surfaceN *= InvertVolumeFactor;
    //   float3 surfaceInWorld = normalize(mul(float4(surfaceN, 0), InverseTransformVolume).xyz);
    //    float3 surfaceInWorld = surfaceN;

    // Reflect if distance changes
    if (sign(distance * distanceNext) < 0 && distance * InvertVolumeFactor > 0)
    {
        float4 rand = hash41u(gi);
        velocity = reflect(velocity, surfaceN + (RandomizeReflection * (rand.xyz - 0.5))) * Bounciness * (RandomizeBounce * (rand.z - 0.5) + 1);
    }
    else
    {
        if (distance * InvertVolumeFactor < 0)
        {
            force = surfaceN * Repulsion;
        }
        else
        {
            force = -surfaceN * Attraction / (1 + distance * AttractionDecay);
        }
        velocity += force;
    }

    if (!isnan(velocity.x) && !isnan(velocity.y) && !isnan(velocity.z))
        Particles[gi].Velocity = velocity;
}
