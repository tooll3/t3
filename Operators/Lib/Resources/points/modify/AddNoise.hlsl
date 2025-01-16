#include "shared/hash-functions.hlsl"
#include "shared/noise-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Frequency;
    float Phase;
    float Variation;

    float3 AmountDistribution;
    float RotationLookupDistance;

    float3 NoiseOffset;
    float __padding;
    // float UseSelection;
}

cbuffer Params : register(b1)
{
    int StrengthMode;
}

StructuredBuffer<Point> SourcePoints : t0;
RWStructuredBuffer<Point> ResultPoints : u0;

float3 GetNoise(float3 pos, float3 variation)
{
    float3 noiseLookup = (pos * 0.91 + variation + Phase) * Frequency;
    return snoiseVec3(noiseLookup) * Amount / 10 * AmountDistribution;
}

static float3 variationOffset;

void GetTranslationAndRotation(float weight, float3 pointPos, float4 rotation,
                               out float3 offset, out float4 newRotation)
{
    float4 noise = float4(GetNoise(pointPos + NoiseOffset, variationOffset), 0);

    offset = noise.xyz * weight;

    float3 xDir = qRotateVec3(float3(RotationLookupDistance, 0, 0), rotation);
    float3 offsetAtPosXDir = GetNoise(pointPos + xDir, variationOffset) * weight;
    float3 rotatedXDir = (pointPos + xDir + offsetAtPosXDir) - (pointPos + offset);

    float3 yDir = qRotateVec3(float3(0, RotationLookupDistance, 0), rotation);
    float3 offsetAtPosYDir = GetNoise(pointPos + yDir, variationOffset) * weight;
    float3 rotatedYDir = (pointPos + yDir + offsetAtPosYDir) - (pointPos + offset);

    float3 rotatedXDirNormalized = normalize(rotatedXDir);
    float3 rotatedYDirNormalized = normalize(rotatedYDir);

    float3 crossXY = cross(rotatedXDirNormalized, rotatedYDirNormalized);
    float3x3 orientationDest = float3x3(
        rotatedXDirNormalized,
        cross(crossXY, rotatedXDirNormalized),
        crossXY);

    newRotation = normalize(qFromMatrix3Precise(transpose(orientationDest)));
}

[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if (i.x >= numStructs)
    {
        return;
    }

    float3 variationOffset = hash41u(i.x).xyz * Variation;

    Point p = SourcePoints[i.x];
    float weight = StrengthMode == 0 ? 1 : ((StrengthMode == 1) ? p.FX1 : p.FX2);

    float3 offset;
    float4 newRotation = p.Rotation;

    GetTranslationAndRotation(weight, p.Position + variationOffset, p.Rotation, offset, newRotation);

    p.Position += offset;
    p.Rotation = newRotation;

    ResultPoints[i.x] = p;
}
