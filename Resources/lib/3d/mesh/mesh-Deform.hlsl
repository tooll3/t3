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
    float __padding1;
    float3 TwistPivot;
    float AbsTaper;
}

StructuredBuffer<PbrVertex> SourceVerts : t0;
RWStructuredBuffer<PbrVertex> ResultVerts : u0;

inline float3 NormalizeToSphere(float3 position, float targetRadius)
{
    float3 posWithPivot = position - Pivot; // Not sure why this isn't working...
    float currentRadius = length(posWithPivot);
    return posWithPivot * (targetRadius / currentRadius);
}

// Tapering function based on selected axis
inline float TaperFunction(float y, float taperAmount)
{
    return 1.0 - taperAmount * y;
}


inline float3 TwistFunction(float3 position, float twistAmount)
{
    float angle;
    float cosA;
    float sinA;
    float3 twisted;
    float3 posWithPivot = position - TwistPivot; 
    switch ((int)TwistAxis)
    {
    case 0:
        angle = posWithPivot.x * twistAmount;
        cosA = cos(angle);
        sinA = sin(angle);
        twisted = float3(posWithPivot.x, posWithPivot.y * cosA - posWithPivot.z * sinA, posWithPivot.y * sinA + posWithPivot.z * cosA);
        break;
    case 1:
        angle = posWithPivot.y * twistAmount;
        cosA = cos(angle);
        sinA = sin(angle);
        twisted = float3(posWithPivot.x * cosA - posWithPivot.z * sinA, posWithPivot.y, posWithPivot.x * sinA + posWithPivot.z * cosA);
        break;
    case 2:
        angle = posWithPivot.z * twistAmount;
        cosA = cos(angle);
        sinA = sin(angle);
        twisted = float3(posWithPivot.x * cosA - posWithPivot.y * sinA, posWithPivot.x * sinA + posWithPivot.y * cosA, posWithPivot.z);
        break;
    }
    return (twisted + TwistPivot);
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

    // Normalize the position to be on a sphere with a desired radius
    float3 spherePos = NormalizeToSphere(pos, Radius);
    pos = lerp(pos, lerp(pos, spherePos, Spherize), s);
    float3 tapered = pos;

    // Apply taper transformation
    switch ((int)TaperAxis)
    {
    case 0:
        tapered.yz *= TaperFunction(pos.x, TaperAmount);
        break;
    case 1:
        tapered.xz *= TaperFunction(pos.y, TaperAmount);
        break;
    case 2:
        tapered.xy *= TaperFunction(pos.z, TaperAmount);
        break;
    }
    pos = lerp(pos, tapered, s);

    //  Apply twist transformation 
    twisted = TwistFunction(pos, TwistAmount) ;

    // Results
    ResultVerts[i.x].Position = lerp(pos, twisted, s);

    ResultVerts[i.x].Normal = SourceVerts[i.x].Normal;
    ResultVerts[i.x].Tangent = SourceVerts[i.x].Tangent;
    ResultVerts[i.x].Bitangent = SourceVerts[i.x].Bitangent;

    ResultVerts[i.x].TexCoord = SourceVerts[i.x].TexCoord;

    ResultVerts[i.x].Selected = SourceVerts[i.x].Selected;
}
