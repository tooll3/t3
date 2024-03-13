#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/pbr.hlsl"

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
    float Space;
    float Direction;
    float OffsetDirection;

    float UseVertexSelection;
    float twistAxis;

}

StructuredBuffer<PbrVertex> SourceVertices : t0;        
RWStructuredBuffer<PbrVertex> ResultVertices : u0;   

float3 GetNoise(float3 pos, float3 variation) 
{
    float3 noiseLookup = (pos * 0.91 + variation) * Frequency +  Phase ;
    float3 noise = snoiseVec3(noiseLookup);
    return (noise + OffsetDirection) * Amount/100 * AmountDistribution ;
}

float3 twistFunction(float3 position, float twistAmount)
{
    float angle;
    float cosA;
    float sinA;
    float3 twisted;
    switch ((int)twistAxis)
    {
        case 0:
         angle = position.x * twistAmount;
         cosA = cos(angle);
         sinA = sin(angle);
         twisted = float3(position.x, position.y * cosA - position.z * sinA,  position.y * sinA + position.z * cosA);
    break;
     case 1:
         angle = position.y * twistAmount;
         cosA = cos(angle);
         sinA = sin(angle);
         twisted = float3(position.x * cosA - position.z * sinA, position.y, position.x * sinA + position.z * cosA);
    break;
     case 2:
         angle = position.z * twistAmount;
         cosA = cos(angle);
         sinA = sin(angle);
         twisted = float3(position.x * cosA - position.y * sinA, position.x * sinA + position.y * cosA, position.z);
    break;
    }
    return twisted;
}

static float3 variationOffset;

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourceVertices.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        return;
    }

    float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    PbrVertex v = SourceVertices[i.x];
    ResultVertices[i.x] = SourceVertices[i.x];

    float weight = 1;
    float3 offset;

    float3 posInWorld = v.Position;
 
    if(Space< 0.5) {
        posInWorld = float3(v.TexCoord.xy, 0);
    }
    else if(Space < 1.5){
        posInWorld = mul(float4(posInWorld ,1), ObjectToWorld).xyz;
    }

    //offset = GetNoise(posInWorld, variationOffset) * weight;
    offset = twistFunction(posInWorld, Amount) ;
    //offset += offset * Amount/100 * 2;

    if(Direction > 0.5)
    {
        float3x3 TBN = float3x3(v.Tangent, v.Bitangent, v.Normal);
        TBN = mul(TBN, (float3x3)ObjectToWorld);
        offset = mul(offset,TBN);
    }

    float selection = UseVertexSelection > 0.5 ? v.Selected : 1;
    offset *= selection;

    float3 newPos = posInWorld + offset;
    float lookUpDistance = RotationLookupDistance / Frequency;

    float3 tAnchor = posInWorld + v.Tangent * lookUpDistance;
    float3 tAnchor2 = posInWorld - v.Tangent * lookUpDistance;

    weight *= selection;
    float3 newTangent  = normalize( tAnchor + twistFunction(tAnchor, Amount) * weight - newPos);
    float3 newTangent2  = -normalize( tAnchor2 + twistFunction(tAnchor2, Amount) * weight - newPos);
    ResultVertices[i.x].Tangent = lerp(newTangent, newTangent2, 0.5);

    float3 bAnchor = posInWorld + v.Bitangent * lookUpDistance;
    float3 bAnchor2 = posInWorld - v.Bitangent * lookUpDistance;

    float3 newBitangent  = normalize( bAnchor + twistFunction(bAnchor, Amount) * weight - newPos);
    float3 newBitangent2  = -normalize( bAnchor2 + twistFunction(bAnchor2, Amount) * weight - newPos);
    ResultVertices[i.x].Bitangent = lerp(newBitangent, newBitangent2, 0.5);

    ResultVertices[i.x].Normal = cross(ResultVertices[i.x].Tangent, ResultVertices[i.x].Bitangent);
    ResultVertices[i.x].Position =  offset;
}

