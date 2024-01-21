#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"

cbuffer Transforms : register(b0)
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
}

cbuffer Params : register(b1)
{
    float4x4 TransformVolume;
    float FallOff;
    float Strength;
    float2 BiasAndGain;
    float Phase;
    float Threshold;
}

cbuffer Params : register(b2)
{
    int VolumeShape;
    int SelectMode;
    int ClampResult;
    int DiscardNonSelected;
    int SetW;
}

StructuredBuffer<Point> SourcePoints : t0;
RWStructuredBuffer<Point> ResultPoints : u0;

static const float NoisePhase = 0;

#define VolumeSphere  0
#define VolumeBox  1
#define VolumePlane  2
#define VolumeZebra  3
#define VolumeNoise  4

#define ModeOverride    0
#define ModeAdd         1
#define ModeSub         2
#define ModeMultiply    3
#define ModeInvert      4

float Bias2(float x, float bias)
{
    return bias < 0
               ? pow(x, clamp(bias + 1, 0.005, 1))
               : 1 - pow(1 - x, clamp(1 - bias, 0.005, 1));
}

inline float LinearStep(float min, float max, float t) 
{
    return saturate((t- min) / (max-min)  );
}

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if (i.x >= numStructs)
    {
        return;
    }

    Point p = SourcePoints[i.x];

    //ResultPoints[i.x] = SourcePoints[i.x];

    if (isnan(p.W))
    {
        ResultPoints[i.x] = p;
        return;
    }

    float3 posInObject = p.Position;

    float3 posInVolume = mul(float4(posInObject, 1), TransformVolume).xyz;

    float s = 1;

    if (VolumeShape == VolumeSphere)
    {
        float distance = length(posInVolume);
        s = LinearStep(1 + FallOff, 1, distance);
        
    }
    else if (VolumeShape == VolumeBox)
    {
        float3 t = abs(posInVolume);
        float distance = max(max(t.x, t.y), t.z) + Phase;
        s = LinearStep(1 + FallOff, 1, distance);
    }
    else if (VolumeShape == VolumePlane)
    {
        float distance = posInVolume.y;
        s = LinearStep(FallOff, 0, distance);
    }
    else if (VolumeShape == VolumeZebra)
    {
        float distance = 1 - abs(mod(posInVolume.y * 1 + Phase, 2) - 1);
        s = LinearStep(Threshold + 0.5 + FallOff, Threshold + 0.5, distance);
    }
    else if (VolumeShape == VolumeNoise)
    {
        float3 noiseLookup = (posInVolume * 0.91 + Phase);
        float noise = snoise(noiseLookup);
        s = LinearStep(Threshold + FallOff, Threshold, noise);
    }

    s = GetBiasGain(s, BiasAndGain.x, BiasAndGain.y);

    float w = p.W;
    if (SelectMode == ModeOverride)
    {
        s *= Strength;
    }
    else if (SelectMode == ModeAdd)
    {
        s += w * Strength;
    }
    else if (SelectMode == ModeSub)
    {
        s = w - s * Strength;
    }
    else if (SelectMode == ModeMultiply)
    {
        s = lerp(w, w * s, Strength);
    }
    else if (SelectMode == ModeInvert)
    {
        s = s * (1 - w);
    }

    p.W = (DiscardNonSelected && s <= 0)
                    ? sqrt(-1)
                : (ClampResult)
                    ? saturate(s)
                    : s;

    ResultPoints[i.x] = p;
}
