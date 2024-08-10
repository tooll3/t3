#include "lib/shared/point.hlsl"

#define THREADS_PER_GROUP 256

cbuffer Params : register(b0)
{
    uint FaceCount; // Computed from summing up chunk lenghts
    uint PointCount;
    uint ChunkIndexForPointsCounts;
};

struct ChunkDef
{
    int StartFaceIndex;
    int FaceCount;
};

struct IndicesForDraw
{
    int PointIndex;
    int3 VertexIndices;
};

StructuredBuffer<uint> ChunkIndicesForPoints : register(t0);
StructuredBuffer<ChunkDef> ChunkDefs : register(t1);
StructuredBuffer<uint> ChunkSizes : register(t2);
StructuredBuffer<uint> ChunkEnds : register(t3);
StructuredBuffer<int3> FaceIndices : register(t4);

RWStructuredBuffer<IndicesForDraw> FaceDrawDatas : register(u0);

[numthreads(THREADS_PER_GROUP, 1, 1)] void UpdateDrawData(uint DTid : SV_DispatchThreadID, uint _GI : SV_GroupIndex)
{
    uint faceIndex = DTid.x;
    if (faceIndex > FaceCount)
        return;

    for (uint pointIndex = 0; pointIndex < PointCount && pointIndex < 100; pointIndex++)
    {
        uint chunkEndFaceIndex = ChunkEnds[pointIndex];
        if (chunkEndFaceIndex < faceIndex)
            continue;

        uint chunkSize = ChunkSizes[pointIndex];
        uint chunkStartFaceIndex = chunkEndFaceIndex - chunkSize;
        uint faceIndexInPointChunk = faceIndex - chunkStartFaceIndex;

        uint chunkDefIndex = ChunkIndicesForPoints[pointIndex % ChunkIndexForPointsCounts];
        uint faceStartIndexForChunk = ChunkDefs[chunkDefIndex].StartFaceIndex;

        FaceDrawDatas[faceIndex].VertexIndices = FaceIndices[faceStartIndexForChunk + faceIndexInPointChunk];
        FaceDrawDatas[faceIndex].PointIndex = pointIndex;
        return;
    }
}
