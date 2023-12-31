#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float VertexCount;
}

StructuredBuffer<int3> SourceFaces : t0;       

RWStructuredBuffer<int3> ResultFaces : u0;   

static float3 variationOffset;

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint faceIndex = i.x;
    uint pointIndex = i.y;
    uint sourcePointCount, sourceFaceCount, stride;

    SourceFaces.GetDimensions(sourceFaceCount, stride);

    if( faceIndex >= sourceFaceCount) {
        return;
    }
    
    uint vertexCount = (int)(VertexCount + 0.5);
    int targetFaceIndex = pointIndex * sourceFaceCount + faceIndex;
    ResultFaces[targetFaceIndex] = SourceFaces[faceIndex] + vertexCount * pointIndex;
}

