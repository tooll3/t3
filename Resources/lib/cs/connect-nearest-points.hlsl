#include "lib/shared/point.hlsl"
#include "lib/shared/hash-functions.hlsl"
#include "lib/points/spatial-hash-map/hash-map-settings.hlsl" 


StructuredBuffer<uint> CellPointIndices :register(t0);     // was "IndexToPointBuffer"
StructuredBuffer<uint2> PointCellIndices :register(t1);    // was "CellIndicesBuffer"
StructuredBuffer<uint> HashGridCells :register(t2);        // was "HashGridBuffer"
StructuredBuffer<uint> CellPointCounts :register(t3);      // was "CountBuffer"
StructuredBuffer<uint> CellRangeIndices :register(t4);     // was "RangeIndexBuffer"
StructuredBuffer<Point> points :register(t5);

RWStructuredBuffer<uint2> pointIndexPairs :register(u0);

#define THREADS_PER_GROUP 512

cbuffer Params : register(b0)
{
    float CurrentStep;           // 0
    float StepCount;             // 1
    float LinesPerSteps;         // 2
    float GridCellSize;  // 3
    float Time;                  // 4
    float ScatterLookUp;         // 5
    float TestIndex;             // 6
}

//static const uint            ParticleGridEntryCount = 4;
//static const uint            ParticleGridCellCount = 20;
//static const float           GridCellSize = 0.1f;


// bool ParticleGridFind(in float3 position, out uint rangeStartIndex, out uint rangeEndIndex)
// {
//     uint i;
//     int3 cell = int3(position / GridCellSize);
//     uint cellIndex = (pcg(cell.x + pcg(cell.y + pcg(cell.z))) % ParticleGridCellCount);
//     uint hashValue = max(xxhash(cell.x + xxhash(cell.y + xxhash(cell.z))), 1);
//     uint cellBegin = cellIndex * ParticleGridEntryCount;
//     uint cellEnd = cellBegin + ParticleGridEntryCount;

//     for(i = cellBegin; i < cellEnd; ++i)
//     {
//         const uint entryValue = HashGridCells[i];
//         if(entryValue == hashValue)
//             break;  // found existing entry
//         if(entryValue == 0)
//             i = cellEnd;
//     }
//     if(i >= cellEnd)
//         return false;

//     rangeStartIndex = CellRangeIndices[i];
//     rangeEndIndex = CellPointCounts[i] + rangeStartIndex;
//     return true;
// }

bool GridFind(in float3 position, out uint startIndex, out uint endIndex)
{
    uint i;
    position+= 100 * GridCellSize;
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
    int count = min(CellPointCounts[i], 50);

    endIndex = startIndex + count;
    return true;
} 

[numthreads( THREADS_PER_GROUP, 1, 1 )]
void ClearPoints(uint3 DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    pointIndexPairs[DTid.x] = int2( 0, 0);
}


[numthreads( THREADS_PER_GROUP, 1, 1 )]
void ConnectPoints(uint3 DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    uint pointCount, __;
    points.GetDimensions(pointCount, __);

    uint pairCount;
    pointIndexPairs.GetDimensions(pairCount, __);

    if(TestIndex >= 0) 
    {
        int testPointIndex = (int)TestIndex;
        float3 position = points[testPointIndex].position;

        uint rangeStartIndex, rangeEndIndex;
        if(GridFind(position, rangeStartIndex, rangeEndIndex)) 
        {
            int x2=0;
            for(uint i=rangeStartIndex; i < rangeEndIndex; ++i) 
            {
                uint otherIndex = CellPointIndices[i];
                pointIndexPairs[DTid.x + x2] = uint2( testPointIndex, otherIndex);
                x2++;
            }
        } 
        return;
    }


    uint indexWithingStep = DTid.x;
    if(indexWithingStep >= LinesPerSteps)
        return;

    uint stepIndex = CurrentStep % StepCount;
    uint pairIndex = stepIndex * (int)(LinesPerSteps + 0.5) + indexWithingStep;

    uint shuffle =  hash11(CurrentStep * 0.123 + indexWithingStep * 0.121 ) * ScatterLookUp * pointCount;
    uint pointIndex = (CurrentStep + shuffle + indexWithingStep ) % pointCount;

    if(pairIndex >= pairCount)
        return;
        

    float3 position = points[pointIndex].position;
    float3 jitter = (hash33u( uint3(DTid.x, DTid.x + 134775813U, DTid.x + 1664525U) + position * 100 + Time % 123.1 ) -0.5f)  * GridCellSize;
    position+= jitter;

    uint rangeStartIndex, rangeEndIndex;
    if(GridFind(position, rangeStartIndex, rangeEndIndex)) 
    {
        const uint particleCount = rangeEndIndex - rangeStartIndex;
        rangeEndIndex = max(rangeStartIndex + 64 , rangeEndIndex);

        float minDistance = 100000;
        uint closestIndex = -1;

        for(uint i=rangeStartIndex ; i < rangeEndIndex; ++i) 
        {
            uint otherIndex = CellPointIndices[i];
            
            if( otherIndex <= DTid.x)
                continue;

            float3 otherPos = points[otherIndex].position;

            float3 direction = position - otherPos;
            float distance = length(position - otherPos);
            
            if(distance < minDistance)
            {
                minDistance = distance;
                closestIndex = otherIndex;
                //break;
            }
        }

        if(closestIndex != -1 || minDistance > 0.01) {

            pointIndexPairs[pairIndex] = int2( closestIndex, pointIndex);
            //pointIndexPairs[pointIndex] = uint2( pointIndex, closestIndex);
            //pointIndexPairs[pairIndex] = uint2( pointIndex, pointIndex+5);
        }
        else {
            pointIndexPairs[pairIndex] = int2( closestIndex, pointIndex);
        }
    }
    
}