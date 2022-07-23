#include "lib/shared/pbr.hlsl"
#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer EmitParameter : register(b0)
{
    float Seed;
};

uint wang_hash(in out uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

struct FaceProperties {
    float normalizedFaceArea;
    float cdf;
};

StructuredBuffer<PbrVertex> Vertices : t0;
StructuredBuffer<int3> FaceIndices : t1;
StructuredBuffer<FaceProperties> CDFs : t2;

RWStructuredBuffer<Point> ResultPoints : u0;

[numthreads(160,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, faceCount, stride;

    ResultPoints.GetDimensions(pointCount, stride);
    FaceIndices.GetDimensions(faceCount, stride);

    if (i.x >= pointCount)
        return; 


    uint rng_state = (i.x * Seed);
    float xi = (float(wang_hash(rng_state)) * (1.0 / 4294967296.0));

    uint stepSize = faceCount /2;
    uint cdfIndex = stepSize;
    
    while (stepSize > 1) 
    {
        stepSize /= 2;                        
        cdfIndex += CDFs[cdfIndex].cdf <= xi 
                     ? stepSize
                     : -stepSize;
    }

    cdfIndex = max( cdfIndex- 4,0);


    while (cdfIndex < faceCount && xi > CDFs[cdfIndex].cdf) // todo: make binary search
    {
         cdfIndex += 1;
    }

    uint faceIndex = cdfIndex;
    if (faceIndex >= (uint)faceCount)
        return;

    float xi1 = (float(wang_hash(rng_state)) * (1.0 / 4294967296.0));
    float xi2 = float(wang_hash(rng_state)) * (1.0 / 4294967296.0);

    uint3 fIndices = FaceIndices[faceIndex];

    // Compute barycentric coordinates
    Point p;
    float xi1Sqrt = sqrt(xi1);
    float u = 1.0 - xi1Sqrt;
    float v = xi2 * xi1Sqrt; 
    float w = 1.0 - u - v;

    p.position = Vertices[fIndices[0]].Position * u
               + Vertices[fIndices[1]].Position * v
               + Vertices[fIndices[2]].Position * w;

    float3 normal = normalize(Vertices[fIndices[0]].Normal * u 
                  + Vertices[fIndices[1]].Normal * v
                  + Vertices[fIndices[2]].Normal * w);

    float3 binormal = normalize(Vertices[fIndices[0]].Bitangent * u 
                    + Vertices[fIndices[1]].Bitangent * v
                    + Vertices[fIndices[2]].Bitangent * w);

    float3 tangent = normalize(Vertices[fIndices[0]].Tangent * u 
                   + Vertices[fIndices[1]].Tangent * v
                   + Vertices[fIndices[2]].Tangent * w);
    
    float3x3 orientationDest= float3x3( tangent,binormal, normal );

    p.rotation = normalize(quaternion_from_matrix_precise(transpose(orientationDest)));
    p.w = 1;

    ResultPoints[i.x] = p;

}

