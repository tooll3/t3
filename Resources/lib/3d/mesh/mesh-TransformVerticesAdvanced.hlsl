#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformMatrix;
    float UseVertexSelection;
    float Spherize;
    float Radius;
    float TaperAmount; // New parameter for tapering
    float TwistAmount; // New parameter for twisting
    float taperAxis;
    float twistAxis;
}

StructuredBuffer<PbrVertex> SourceVerts : t0;        
RWStructuredBuffer<PbrVertex> ResultVerts : u0;   

float3 normalizeToSphere(float3 position, float targetRadius)
{
    float currentRadius = length(position);
    return position * (targetRadius / currentRadius);
}

// Tapering function based on Y-coordinate
float taperFunction(float y, float taperAmount)
{
    return 1.0 - taperAmount * y;
}

// Twist function based on Y-coordinate
/* float twistFunction(float y, float twistAmount)
{
    return y * twistAmount;
} */

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


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourceVerts.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        return;
    }
    
    float s = UseVertexSelection > 0.5 ? SourceVerts[i.x].Selected : 1;

    float3 pos = SourceVerts[i.x].Position;


    // Normalize the position to be on a sphere with a desired radius (e.g., 1.0)
    float3 Spos = normalizeToSphere(pos, Radius);
    pos = lerp(pos, Spos, Spherize);

    // Apply taper transformation 
   switch ((int)taperAxis)
    {
     case 0:
        pos.yz *= taperFunction(pos.x, TaperAmount);
        break;
    case 1:
        pos.xz *= taperFunction(pos.y, TaperAmount);
        break;
    case 2:
        pos.xy *= taperFunction(pos.z, TaperAmount);
        break;
    }

    // Apply twist transformation based on Y-coordinate
    /* float twistAngle = twistFunction(pos.y, TwistAmount);
    float cosTheta = cos(twistAngle);
    float sinTheta = sin(twistAngle);
    pos.xz = float2(pos.x * cosTheta - pos.z * sinTheta, pos.x * sinTheta + pos.z * cosTheta); */
    pos = twistFunction(pos, TwistAmount);

 

    ResultVerts[i.x].Position = lerp(pos, mul(float4(pos,1), TransformMatrix).xyz, s);

     // Transform normal without normalization
    float3 normal = SourceVerts[i.x].Normal;
    ResultVerts[i.x].Normal = lerp(normal, normalize(mul(float4(normal, 0), TransformMatrix).xyz), s);

    float3 tangent = SourceVerts[i.x].Tangent;
    ResultVerts[i.x].Tangent = lerp(tangent, normalize(mul(float4(tangent, 0), TransformMatrix).xyz), s);

    float3 bitangent = SourceVerts[i.x].Bitangent;
    ResultVerts[i.x].Bitangent = lerp(bitangent, normalize(mul(float4(bitangent, 0), TransformMatrix).xyz), s);

    ResultVerts[i.x].TexCoord = SourceVerts[i.x].TexCoord;

    ResultVerts[i.x].Selected = SourceVerts[i.x].Selected;
}

