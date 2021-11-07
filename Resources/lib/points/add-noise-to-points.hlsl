#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

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
    float Amount;
    float Frequency;
    float Phase;
    float Variation;
    float3 AmountDistribution;
    float RotationLookupDistance;
    float UseWAsWeight;

}

StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

float3 GetNoise(float3 pos, float3 variation) 
{
    float3 noiseLookup = (pos * 0.91 + variation + Phase ) * Frequency;
    return snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution;
}

static float3 variationOffset;

void GetTranslationAndRotation(float weight, float3 pointPos, float4 rotation, 
                               out float3 offset, out float4 newRotation) 
{    
    offset = GetNoise(pointPos, variationOffset) * weight;

    float3 xDir = rotate_vector(float3(RotationLookupDistance,0,0), rotation);
    float3 offsetAtPosXDir = GetNoise(pointPos + xDir, variationOffset) * weight;
    float3 rotatedXDir = (pointPos + xDir + offsetAtPosXDir) - (pointPos + offset);

    float3 yDir = rotate_vector(float3(0, RotationLookupDistance,0), rotation);
    float3 offsetAtPosYDir = GetNoise(pointPos + yDir, variationOffset) * weight;
    float3 rotatedYDir = (pointPos + yDir + offsetAtPosYDir) - (pointPos + offset);

    float3 rotatedXDirNormalized = normalize(rotatedXDir);
    float3 rotatedYDirNormalized = normalize(rotatedYDir);
    
    float3 crossXY = cross(rotatedXDirNormalized, rotatedYDirNormalized);
    float3x3 orientationDest= float3x3(
        rotatedXDirNormalized, 
        cross(crossXY, rotatedXDirNormalized), 
        crossXY );

    newRotation = normalize(q_from_matrix(transpose(orientationDest)));
}

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        ResultPoints[i.x].w = 0 ;
        return;
    }


    float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    Point p = SourcePoints[i.x];

    float weight = UseWAsWeight < 0 ? lerp(1, 1- p.w, -UseWAsWeight) 
                                : lerp(1, p.w, UseWAsWeight);

    float3 offset;;
    float4 newRotation = p.rotation;

    float4 posInWorld = mul(float4(p.position ,1), ObjectToWorld);
    GetTranslationAndRotation(weight , posInWorld.xyz + variationOffset, p.rotation, offset, newRotation);

    ResultPoints[i.x].position = p.position + offset ;
    ResultPoints[i.x].rotation = newRotation;

    ResultPoints[i.x].w =  SourcePoints[i.x].w ;
}

