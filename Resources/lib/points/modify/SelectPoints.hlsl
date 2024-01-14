#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

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
};

cbuffer Params : register(b1)
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
    float DiscardNonSelected;
}

StructuredBuffer<Point> SourcePoints : t0;
RWStructuredBuffer<Point> ResultPoints : u0;

static const float NoisePhase = 0;

static const float VolumeSphere = 0.5;
static const float VolumeBox = 1.5;
static const float VolumePlane = 2.5;
static const float VolumeZebra = 3.5;
static const float VolumeNoise = 4.5;

static const float ModeOverride = 0.5;
static const float ModeAdd = 1.5;
static const float ModeSub = 2.5;
static const float ModeMultiply = 3.5;
static const float ModeInvert = 4.5;

float Bias2(float x, float bias)
{
    return bias < 0
               ? pow(x, clamp(bias + 1, 0.005, 1))
               : 1 - pow(1 - x, clamp(1 - bias, 0.005, 1));
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

    if (!isnan(p.W))
    {
        float3 posInObject = p.Position;

        float3 posInVolume = mul(float4(posInObject, 1), TransformVolume).xyz;

        float s = 1;

        if (VolumeShape < VolumeSphere)
        {
            float distance = length(posInVolume);
            s = smoothstep(1 + FallOff, 1, distance);
        }
        else if (VolumeShape < VolumeBox)
        {
            float3 t = abs(posInVolume);
            float distance = max(max(t.x, t.y), t.z) + Phase;
            s = smoothstep(1 + FallOff, 1, distance);
        }
        else if (VolumeShape < VolumePlane)
        {
            float distance = posInVolume.y;
            s = smoothstep(FallOff, 0, distance);
        }
        else if (VolumeShape < VolumeZebra)
        {
            float distance = 1 - abs(mod(posInVolume.y * 1 + Phase, 2) - 1);
            s = smoothstep(Threshold + 0.5 + FallOff, Threshold + 0.5, distance);
        }
        else if (VolumeShape < VolumeNoise)
        {
            float3 noiseLookup = (posInVolume * 0.91 + Phase);
            float noise = snoise(noiseLookup);
            s = smoothstep(Threshold + FallOff, Threshold, noise);
        }

        s = Bias2(s, Bias);

        float selected = p.Selected;
        if (SelectMode < ModeOverride)
        {
            s *= Strength;
        }
        else if (SelectMode < ModeAdd)
        {
            s += selected * Strength;
        }
        else if (SelectMode < ModeSub)
        {
            s = selected - s * Strength;
        }
        else if (SelectMode < ModeMultiply)
        {
            s = lerp(selected, selected * s, Strength);
        }
        else if (SelectMode < ModeInvert)
        {
            s = s * (1 - selected);
        }

        p.Selected = (DiscardNonSelected > 0.5 && s <= 0)
                        ? sqrt(-1)
                    : (ClampResult > 0.5)
                        ? saturate(s)
                        : s;
    }

    ResultPoints[i.x] = p;
}
