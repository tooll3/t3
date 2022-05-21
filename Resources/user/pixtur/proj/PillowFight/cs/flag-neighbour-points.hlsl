#include "lib/shared/point.hlsl"
#include "hash-functions.hlsl"
//#include "lib/points/spatial-hash-map/spatial-hash-map.hlsl"

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

    float Time;
    float CenterPointIndex;
}

static const uint            ParticleGridEntryCount = 4;
static const uint            ParticleGridCellCount = 20;


bool GridFind(in float3 position, out uint startIndex, out uint endIndex)
{
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

    //float3 position = points[DTid.x].position;

    //if(DTid.x != (int)(CenterPointIndex) % pointCount )
    //    return;

    /*
    // Look up by particle index    
    const uint2 gridCell = GridCellBuffer[DTid.x];
    const uint cellIndex = gridCell.x;
    const uint gridEntryIndex =  gridCell.y;

    const uint rangeStartIndex = RangeIndexBuffer[cellIndex];
    const uint rangeLength = GridCountBuffer[cellIndex];
    const uint endIndex = rangeStartIndex + rangeLength;
    for(uint i=rangeStartIndex; i < endIndex; ++i) 
    {
        uint pointIndex = GridBuffer[i];
        points[pointIndex].w = 0.6;
    }
    */
    
    if(DTid.x != 0) 
        return;


    // Lookby by position
    float3 position = Center + 0.0;
    
    //float3 position = points[DTid.x].position;
    //float3 jitter = (hash33u( uint3(DTid.x, DTid.x + 134775813U, DTid.x + 1664525U) + position * 1000 + Time * 934 % 123.123 ) -0.5f)  * GridCellSize; // - (GridCellSize * 0.5);
    //position+= jitter;

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