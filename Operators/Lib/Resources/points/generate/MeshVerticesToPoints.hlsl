#include "shared/hash-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float3 OffsetByTBN;
    float OffsetScale;
}

// struct PbrVertex
// {
//     float3 Position;
//     float3 Normal;
//     float3 Tangent;
//     float3 Bitangent;
//     float2 TexCoord;
//     float2 __padding;
// };

StructuredBuffer<PbrVertex> Vertices : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output


float4 quad_from_Mat3(float3 col0, float3 col1, float3 col2)
{
    /* warning - this only works when the matrix is orthogonal and spacially orthogonal */
    float w = sqrt(1.0f + col0.x + col1.y + col2.z) / 2.0f;

    return float4(
        (col1.z - col2.y) / (4.0f * w),
        (col2.x - col0.z) / (4.0f * w),
        (col0.y - col1.x) / (4.0f * w),
        w);
}



[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint index = i.x; 
    PbrVertex v = Vertices[index];

    ResultPoints[index].Position = v.Position 
        + OffsetByTBN.x * v.Tangent * OffsetScale 
        + OffsetByTBN.y * v.Bitangent * OffsetScale
        + OffsetByTBN.z * v.Normal * OffsetScale;

    ResultPoints[index].W = v.Selected;
    
    // Faster be incorrect rotations
    //float4 rot = quad_from_Mat3(m[0], m[1], m[2]);
    //float3x3 m = float3x3(v.Tangent, v.Bitangent, v.Normal);
    
    float3x3 m = float3x3(v.Tangent, v.Bitangent,v.Normal);
    float4 rot = normalize(qFromMatrix3Precise(transpose(m)));
    ResultPoints[index].Rotation = normalize(rot);
    ResultPoints[index].Color =1;
    ResultPoints[index].Selected = v.Selected;
}