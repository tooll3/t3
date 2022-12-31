#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float Range;
    float Offset;
    float Scale;
}

StructuredBuffer<PbrVertex> SourceVertices : t0;
StructuredBuffer<Point> SourcePoints : t1;
StructuredBuffer<Point> TargetPoints : t2;

RWStructuredBuffer<PbrVertex> ResultVertices : u0;



// Define weights for controlpoints in clockwise order
// Y pointsup
static const float2 PointWeights[4] = {
    float2(0,1),    // Top left
    float2(1,1),    // Top Right
    float2(0,1),    // Bottom right
    float2(0,0),    // Bottom left
};


// static float f;
// static float3 posA;
// static float3 posB;
// static float4x4 orientationA;
// static float4x4 orientationB;

// float3 TransformVector(float3 v) {
//     float3 v2 = float3(0,v.yz) * Scale + lerp(posA, posB, f);
//     v2 = lerp(mul( float4(v2 - posA, 1), orientationA).xyz + posA,
//               mul( float4(v2 - posB, 1), orientationB).xyz + posB,
//               f);
//     return v2;
// }

// float3 TransformDirection(float3 v) {
//     // float3 v2 = float3(0,v.yz) + lerp(posA, posB, f);
//     // v2 = lerp(mul( float4(v2 - posA, 0), orientationA).xyz,
//     //           mul( float4(v2 - posB, 0), orientationB).xyz,
//     //           f);

//     return lerp( mul( float4(v, 0), orientationA).xyz,
//     mul( float4(v, 0), orientationA).xyz,
//     f);

// }

static const int MaxControlPointCount = 10;



float GetWeight(float2 delta) 
{
    //return  pow(1/length(delta) , 1.3);

    float d = length(delta);
    if(d < 0.000001)
        return 9999999;
    
    return  1/d;
}

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint vertexIndex = i.x;

    uint vertexCount, sourcePointCount, targetPointCount, stride;
    SourceVertices.GetDimensions(vertexCount, stride);
    SourcePoints.GetDimensions(sourcePointCount, stride);
    TargetPoints.GetDimensions(targetPointCount, stride);

    if(vertexIndex >= vertexCount) {
        return;
    }


    // float weight = 1;
    // float3 offset;

    PbrVertex v = SourceVertices[vertexIndex];

    float3 posInWorld = v.Position;

    int maxPointCount = min(min(sourcePointCount, targetPointCount), MaxControlPointCount);

    float2 maxD= 0;
    float maxD2= 0;

    // float2 Distances[MaxControlPointCount]; // Cache distances to avoid recomputation
    // float2 Weights[MaxControlPointCount]; // Cache distances to avoid recomputation

    // Get max weight
    int pi;

    float weightSum=0;

    for(pi = 0; pi < maxPointCount; pi++) 
    {
        // float2 d= float2(
        //     length(v.Position.x - SourcePoints[pi].position.x),
        //     length(v.Position.y - SourcePoints[pi].position.y)
        // );
        //float weight = GetWeight(v.Position.xy - SourcePoints[pi].position.xy);

        // float d = length(v.Position.xy - SourcePoints[pi].position.xy);

        // // Distances[pi]= d;

        // float weight =  d> 0.00001 ? 1/d : 999999999;
        // // Weights[pi] = weight;

        weightSum += GetWeight(v.Position.xy - SourcePoints[pi].position.xy);
        // maxD = max(d, maxD);        
        // maxD2 = max(length(d), length(maxD2));
    }

    // Transform with weights
    float3 offsetSum = 0;
    float2 weightsSum =0;
    for(pi = 0; pi < maxPointCount; pi++) 
    {
        float3 targetPointPos = TargetPoints[pi].position;
        float3 offset = targetPointPos - SourcePoints[pi].position;
        float weightP = GetWeight(v.Position.xy - SourcePoints[pi].position.xy) / weightSum;

        offsetSum += offset * weightP;    // TODO: Clarify what to do with Z. Maybe average?

        float3 pInPointSpace = v.Position - targetPointPos;


        // Matrix t = transpose(quaternion_to_matrix(TargetPoints[pi].rotation));

        // float3 p2 =  mul( float4(pInPointSpace, 0), t).xyz;
        // p2 += targetPointPos + offset * weightP;
        // offsetSum += p2;
    }

    v.Position += offsetSum;

    // float floatIndex = posInWorld.x * (Range) * pointCount * Scale   + Offset * pointCount + 0.00001;

    // uint aIndex = (int)clamp(floatIndex, 0, pointCount-2);
    // uint bIndex = aIndex + 1;
    // f = floatIndex - aIndex;

    // Point pointA = SourcePoints[aIndex];
    // Point pointB = SourcePoints[bIndex];

    // orientationA = transpose(quaternion_to_matrix(pointA.rotation));
    // orientationB = transpose(quaternion_to_matrix(pointB.rotation));
    // posA = pointA.position;
    // posB = pointB.position;

    // v.Position = TransformVector(v.Position);

    // v.Normal = normalize(TransformDirection(v.Normal));
    // v.Tangent = normalize(TransformDirection(v.Tangent));
    // v.Bitangent = normalize(TransformDirection(v.Bitangent));

    ResultVertices[vertexIndex] = v;
}

