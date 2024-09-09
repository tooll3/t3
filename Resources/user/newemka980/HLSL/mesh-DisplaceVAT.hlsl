//#include "lib/shared/hash-functions.hlsl"
//#include "lib/shared/noise-functions.hlsl"
//#include "lib/shared/point.hlsl"
//#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float Mode;
    float Amount;
    float2 ScaleUV;

    float3 Distribution;
    float UseVertexSelection;

    float3 MainOffset;
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



StructuredBuffer<PbrVertex> SourceVertices : t0;        
Texture2D<float4> DisplaceMap : register(t1);
Texture2D<float4> NormalMap : register(t2);


RWStructuredBuffer<PbrVertex> ResultVertices : u0;   

sampler texSampler : register(s0);

[numthreads(80,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    uint pointCount, _;
    SourceVertices.GetDimensions(pointCount, _);
    if(gi >= pointCount) {
        return;
    }

    PbrVertex v = SourceVertices[gi];
    ResultVertices[gi] = SourceVertices[gi];

    float weight = 1;

    float3 posInWorld = v.Position;
 
    float2 uv = SourceVertices[gi].TexCoord2 * ScaleUV;

    uv += MainOffset.xy;
    float4 texColor = DisplaceMap.SampleLevel(texSampler, uv, 0); 
    float4 normals = NormalMap.SampleLevel(texSampler, uv, 0);
    float3x3 TBN = float3x3(v.Tangent, v.Bitangent, v.Normal);

    
    float3 offset = 0;
    
    offset= texColor.rbg * Distribution;
    //v.Normal = normals.rgb;
    //ResultVertices[gi].Tangent = 0;
    //ResultVertices[gi].Normal = v.Normal  ;
    ResultVertices[gi].Position = v.Position + offset;
    ResultVertices[gi].TexCoord = SourceVertices[gi].TexCoord;



    float3 bitangent = SourceVertices[gi].Bitangent;
    float3 tangent = SourceVertices[gi].Tangent;
    float3 orgNormal = SourceVertices[gi].Normal;
    float3 newNormal = normals.rbg;

    float3 newTangent = cross(bitangent, newNormal);
    float3 newBitangent = cross(newNormal, newTangent);


    ResultVertices[gi].Normal =  newNormal;
 
}