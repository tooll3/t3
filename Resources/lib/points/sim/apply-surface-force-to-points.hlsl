#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformVolume;

    float FallOff;
    float Bias;
    float VolumeShape;
    float SelectMode;

    float ClampResult;
    float Strength;
    float Phase;
    float Threshold;

    float Bounciness;
    float Attraction;
    float AttractionDecay;
    float MaxAcceleration;

    float Repulsion;
}

RWStructuredBuffer<Point> Points : u0; 

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
    uint numStructs, stride;
    Points.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) 
        return;

    if (isnan(Points[i.x].w))
        return;
        
    float3 pos = Points[i.x].position;
    float4 rot = Points[i.x].rotation;


    float4 normalizedRot;
    float v = q_separate_v(rot, normalizedRot);
    float3 forward = rotate_vector(float3(0,0, v), normalizedRot);
    float3 posInVolume = mul(float4(pos, 1), TransformVolume).xyz;
    //v = 1;
    float3 posInVolumeNext = mul(float4(pos + forward *v * 0.01, 1), TransformVolume).xyz;

    float s = 1;

    // if (VolumeShape < VolumeSphere)
    // {
    //     float distance = length(posInVolume);
    //     s = smoothstep(1 + FallOff, 1, distance);
    // }
    // else if (VolumeShape < VolumeBox)
    // {
    //     float3 t = abs(posInVolume);
    //     float distance = max(max(t.x, t.y), t.z) + Phase;
    //     s = smoothstep(1 + FallOff, 1, distance);
    // }
    // else if (VolumeShape < VolumePlane)
    // {
    //     float distance = posInVolume.y;
    //     s = smoothstep(FallOff, 0, distance);
    // }
    // else if (VolumeShape < VolumeZebra)
    // {
    //     float distance = 1 - abs(mod(posInVolume.y * 1 + Phase, 2) - 1);
    //     s = smoothstep(Threshold + 0.5 + FallOff, Threshold + 0.5, distance);
    // }
    // else if (VolumeShape < VolumeNoise)
    // {
    //     float3 noiseLookup = (posInVolume * 0.91 + Phase);
    //     float noise = snoise(noiseLookup);
    //     s = smoothstep(Threshold + FallOff, Threshold, noise);
    // }

    //float3 force =0;
    float3 force = float3(0,-0.01,0);
    float rUnitSphere = 0.5;
    float distance = length(posInVolume) - rUnitSphere;
    float distance2 = length(posInVolumeNext) - rUnitSphere;
    float3 surfaceN = normalize(posInVolume);
    
    if(sign( distance * distance2) <0  && distance > 0) 
    {
        //Points[i.x].position += float3(1,0,0);
        forward = reflect(forward, surfaceN) * Bounciness;

    } 
    else 
    {
        if(distance < 0) {
            force = surfaceN * Repulsion;
        }
        else 
        {
            force = -surfaceN * Attraction;

        }
        forward += force;
    }   
    //Points[i.x].w =  distance2 - distance;

    // s = clamp( (smoothstep(1 + FallOff, 1- FallOff, distance) - 0.5),-10,0) * Bounciness;
    // float3 force = posInVolume/(distance+0.0001) *s;

    float newV = length(forward);
    // if(newV == 0) {
    //     forward= float3(0,0,1);
    // }
    float4 newRotation = q_look_at(normalize(forward), float3(0,0,1));
    Points[i.x].rotation = q_encode_v(newRotation, newV);    
}

