#include "shared/hash-functions.hlsl"
#include "shared/noise-functions.hlsl"
#include "shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformMatrix;
    float UseVertexSelection;
}

StructuredBuffer<PbrVertex> SourceVerts : t0;        
RWStructuredBuffer<PbrVertex> ResultVerts : u0;   


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourceVerts.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        return;
    }
    
    float s = UseVertexSelection > 0.5 ? SourceVerts[i.x].Selected : 1;
    float3 pos = float3(SourceVerts[i.x].TexCoord, 0);
    ResultVerts[i.x] = SourceVerts[i.x];

    ResultVerts[i.x].TexCoord = lerp(pos, mul(float4(pos,1), TransformMatrix).xyz, s).xy;
}

