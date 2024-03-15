#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float UseVertexSelection;
    float Spherize;
    float Radius;
    float TaperAmount; // New parameter for tapering

    float TwistAmount; // New parameter for twisting
    float TaperAxis;
    float TwistAxis;
    float __padding;

    float3 Pivot;
}

StructuredBuffer<PbrVertex> SourceVerts : t0;
RWStructuredBuffer<PbrVertex> ResultVerts : u0;

inline float3 NormalizeToSphere(float3 position, float targetRadius)
{
    float3 posWithPivot = position - Pivot; // Not sure why this isn't working...
    float currentRadius = length(posWithPivot);
    return posWithPivot * (targetRadius / currentRadius);
}

// Tapering function based on Y-coordinate
inline float TaperFunction(float y, float taperAmount)
{
    return 1.0 - taperAmount * y;
}

// Twist function based on Y-coordinate
/* float TwistFunction(float y, float twistAmount)
{
    return y * twistAmount;
} */

inline float3 TwistFunction(float3 position, float twistAmount)
{
    float angle;
    float cosA;
    float sinA;
    float3 twisted;
    switch ((int)TwistAxis)
    {
    case 0:
        angle = position.x * twistAmount;
        cosA = cos(angle);
        sinA = sin(angle);
        twisted = float3(position.x, position.y * cosA - position.z * sinA, position.y * sinA + position.z * cosA);
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

[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourceVerts.GetDimensions(numStructs, stride);
    if (i.x >= numStructs)
    {
        return;
    }

    float s = UseVertexSelection > 0.5 ? SourceVerts[i.x].Selected : 1;

    float3 pos = SourceVerts[i.x].Position;

    float3 twisted = SourceVerts[i.x].Position; // for twist

    // Normalize the position to be on a sphere with a desired radius (e.g., 1.0)
    float3 spherePos = NormalizeToSphere(pos, Radius);
    // pos = lerp(pos, spherePos, Spherize);
    pos = lerp(pos, lerp(pos, spherePos, Spherize), s);

    // Apply taper transformation
    switch ((int)TaperAxis)
    {
    case 0:
        pos.yz *= TaperFunction(pos.x, TaperAmount);
        break;
    case 1:
        pos.xz *= TaperFunction(pos.y, TaperAmount);
        break;
    case 2:
        pos.xy *= TaperFunction(pos.z, TaperAmount);
        break;
    }
    // pos = lerp(pos, tapered, s);
    //  Apply twist transformation based on Y-coordinate
    /* float twistAngle = TwistFunction(pos.y, TwistAmount);
    float cosTheta = cos(twistAngle);
    float sinTheta = sin(twistAngle);
    pos.xz = float2(pos.x * cosTheta - pos.z * sinTheta, pos.x * sinTheta + pos.z * cosTheta); */
    twisted = TwistFunction(pos, TwistAmount);

    ResultVerts[i.x].Position = lerp(pos, twisted, s);

    // ResultVerts[i.x].Position = lerp(pos, dpos,s) + lerp(pos, mul(float4(pos,1), TransformMatrix).xyz, s);

    // Transform normal without normalization

    // float3 normal = SourceVerts[i.x].Normal;
    // ResultVerts[i.x].Normal = lerp(normal, normalize(mul(float4(normal, 0), TransformMatrix).xyz), s);

    // float3 tangent = SourceVerts[i.x].Tangent;
    // ResultVerts[i.x].Tangent = lerp(tangent, normalize(mul(float4(tangent, 0), TransformMatrix).xyz), s);

    // float3 bitangent = SourceVerts[i.x].Bitangent;
    // ResultVerts[i.x].Bitangent = lerp(bitangent, normalize(mul(float4(bitangent, 0), TransformMatrix).xyz), s);

    ResultVerts[i.x].Normal = SourceVerts[i.x].Normal;
    ResultVerts[i.x].Tangent = SourceVerts[i.x].Tangent;
    ResultVerts[i.x].Bitangent = SourceVerts[i.x].Bitangent;

    ResultVerts[i.x].TexCoord = SourceVerts[i.x].TexCoord;

    ResultVerts[i.x].Selected = SourceVerts[i.x].Selected;
}
