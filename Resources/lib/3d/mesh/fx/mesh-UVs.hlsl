//#include "lib/shared/hash-functions.hlsl"
// #include "lib/shared/point.hlsl"
//#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float BlendFactor;
}

StructuredBuffer<PbrVertex> VerticesA : t0;        // input

RWStructuredBuffer<PbrVertex> ResultVertices : u0; // output

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint resultCount, countA, countB, stride;
    ResultVertices.GetDimensions(resultCount, stride);
    VerticesA.GetDimensions(countA, stride);


    if (i.x > resultCount)
        return;

    uint aIndex = i.x;

    float t = i.x / (float)resultCount;

    PbrVertex A = VerticesA[aIndex];

    float f = BlendFactor;

    ResultVertices[i.x].Position = lerp(A.Position, float3(A.TexCoord,0) , f);
    ResultVertices[i.x].Normal = lerp(A.Normal,float3(0,0,1), f); 
    ResultVertices[i.x].Tangent = lerp(A.Tangent, float3(1,0,0), f);
    ResultVertices[i.x].Bitangent = lerp(A.Bitangent, float3(0,1,0), f);
    ResultVertices[i.x].TexCoord = A.TexCoord;
    ResultVertices[i.x].Selected = A.Selected;
}
