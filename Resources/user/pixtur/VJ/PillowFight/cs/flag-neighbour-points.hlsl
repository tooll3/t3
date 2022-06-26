#include "lib/shared/point.hlsl"
#include "lib/shared/hash-functions.hlsl"
#include "lib/points/spatial-hash-map/hash-map-settings.hlsl" 

StructuredBuffer<uint> CellPointIndices :register(t0);         // IndexToPointBuffer
StructuredBuffer<uint2> PointCellIndices :register(t1);    // CellIndicesBuffer -> PointCellIndices
StructuredBuffer<uint> HashGridCells :register(t2);     // HashGridBuffer -> HashGridCells
StructuredBuffer<uint> CellPointCounts :register(t3);    // CountBuffer -> CellPointCounts
StructuredBuffer<uint> CellRangeIndices :register(t4);    // RangeIndexBuffer -> CellRangeIndices
 
RWStructuredBuffer<Point> points :register(u0); 

cbuffer Params : register(b0)
{
    float3 Center; 
    float GridCellSize;
}

//static const uint            ParticleGridCellCount = 2000;
//static const uint            ParticleGridEntryCount = 16;


bool GridFind(in float3 position, out uint startIndex, out uint endIndex)
{
    position += 100 * GridCellSize;
    uint i;
    int3 cell = int3(position / GridCellSize);
    uint cellIndex = (pcg(cell.x + pcg(cell.y + pcg(cell.z))) % ParticleGridCellCount);
    uint hashValue = max(xxhash(cell.x + xxhash(cell.y + xxhash(cell.z))), 1);
    uint cellBegin = cellIndex * ParticleGridEntryCount;
    uint cellEnd = cellBegin + ParticleGridEntryCount;
    for(i = cellBegin; i < cellEnd; ++i)
    {
        const uint entryValue = HashGridCells[i];
        if(entryValue == hashValue)
            break;  // found existing entry

        if(entryValue == 0)
            i = cellEnd;
    }
    if(i >= cellEnd)
        return false;

    startIndex = CellRangeIndices[i];
    endIndex = startIndex + CellPointCounts[i];
    return true;
} 


[numthreads( 256, 1, 1 )]
void FlagPoints(uint3 DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    uint pointCount, stride;
    points.GetDimensions(pointCount, stride);
        
    if(DTid.x >= pointCount)
        return; // out of bounds

    
    if(DTid.x != 0) 
        return;

    float3 position = Center + 0.0;

    int startIndex, endIndex;
    if(GridFind(position, startIndex, endIndex)) 
    {
        for(uint i=startIndex; i < endIndex; ++i) 
        {
            uint pointIndex = CellPointIndices[i];
            points[pointIndex].w = 1;
        }
    } 
       
}