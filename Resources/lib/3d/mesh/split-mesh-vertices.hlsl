#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "pbr.hlsl"

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
    float FlatShading;
}

StructuredBuffer<int3> SourceIndices : t0;
StructuredBuffer<PbrVertex> SourceVertices : t1;

RWStructuredBuffer<int3> ResultIndices : u0;
RWStructuredBuffer<PbrVertex> ResultVertices : u1;


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numFaces, stride;
    SourceIndices.GetDimensions(numFaces, stride);
    int faceIndex = i.x;
    if(faceIndex >= numFaces) {
        return;
    }

    int3 indicesForFace = SourceIndices[faceIndex];
    
    int vertexIndex = faceIndex* 3;

    ResultVertices[vertexIndex+0] = SourceVertices[indicesForFace.x];
    ResultVertices[vertexIndex+1] = SourceVertices[indicesForFace.y];
    ResultVertices[vertexIndex+2] = SourceVertices[indicesForFace.z];

    ResultIndices[faceIndex] = int3(vertexIndex, vertexIndex+1, vertexIndex+2);
}

