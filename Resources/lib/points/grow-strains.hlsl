#include "point.hlsl"
#include "noise-functions.hlsl"
#include "hash-functions.hlsl"

cbuffer Params : register(b0)
{
    float Variation;
    float NoiseAmount;
    float Frequency;
    float Phase;    

    float3 NoiseDistribution;
    float RotationLookupDistance;

    float Length;
    float Width;
    float NoiseDensity;
}

StructuredBuffer<Point> PointsA : t0;
StructuredBuffer<Point> PointsB : t1;

Texture2D<float4> GrowthMap : register(t2);
sampler texSampler : register(s0);

RWStructuredBuffer<Point> ResultPoints : u0;


float3 GetNoise(float3 pos) 
{
    float3 noiseLookup = (pos + Phase ) * Frequency;
    return snoiseVec3(noiseLookup) * NoiseAmount * NoiseDistribution;
}

void GetTranslationAndRotation(float weight, float3 pointPos, float4 rotation, 
                               out float3 offset, out float4 newRotation) 
{    
    offset = GetNoise(pointPos) * weight;

    float3 xDir = rotate_vector(float3(RotationLookupDistance,0,0), rotation);
    float3 offsetAtPosXDir = GetNoise(pointPos + xDir) * weight;
    float3 rotatedXDir = (pointPos + xDir + offsetAtPosXDir) - (pointPos + offset);

    float3 yDir = rotate_vector(float3(0, RotationLookupDistance,0), rotation);
    float3 offsetAtPosYDir = GetNoise(pointPos + yDir) * weight;
    float3 rotatedYDir = (pointPos + yDir + offsetAtPosYDir) - (pointPos + offset);

    float3 rotatedXDirNormalized = normalize(rotatedXDir);
    float3 rotatedYDirNormalized = normalize(rotatedYDir);
    
    float3 crossXY = cross(rotatedXDirNormalized, rotatedYDirNormalized);
    float3x3 orientationDest= float3x3(
        rotatedXDirNormalized, 
        cross(crossXY, rotatedXDirNormalized), 
        crossXY );

    newRotation = normalize(q_from_matrix(transpose(orientationDest)));
}



[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint sourceCount, strideA;
    PointsA.GetDimensions(sourceCount, strideA);
    sourceCount +=1; //For NaN-Separator

    uint targetPosCount, strideB;
    PointsB.GetDimensions(targetPosCount, strideB);

    //uint sourceCount = (uint)(CountA + 0.1);  
    uint sourceIndex = i.x % sourceCount;
    
    if(sourceIndex == sourceCount-1) 
    {
        ResultPoints[i.x].w = sqrt(-1);
    }
    else 
    {
        uint targetIndex = (i.x / sourceCount )  % targetPosCount;
        Point A = PointsA[sourceIndex];
        Point B = PointsB[targetIndex];

        //ResultPoints[i.x] = A;
        //return;

        float3  pLocal = rotate_vector(A.position, B.rotation);

        float age = B.w;
        float w = A.w;

        float4 attributes = GrowthMap.SampleLevel(texSampler, float2(age,1-w), 0);
        float d = saturate(attributes.r - 0.05);
        if(d < 0.001)
            d = sqrt(-1);

        float4 rotation = qmul(A.rotation, B.rotation);

        float noiseWeight = attributes.g;
        float3 offset;
        float4 newRotation;
        float3 variationOffset = hash31(targetIndex) * Variation;

        GetTranslationAndRotation(noiseWeight, pLocal * NoiseDensity + variationOffset, rotation, offset, newRotation);

        ResultPoints[i.x].position = pLocal * Length + B.position + offset;

        ResultPoints[i.x].w = d * Width;
        ResultPoints[i.x].rotation = newRotation;
    }
}
