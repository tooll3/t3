#include "lib/shared/point.hlsl"
#include "lib/shared/hash-functions.hlsl"

StructuredBuffer<uint> particleGridBuffer :register(t0);
StructuredBuffer<uint> particleGridCellBuffer :register(t1);
StructuredBuffer<uint> particleGridHashBuffer :register(t2);
StructuredBuffer<uint> particleGridCountBuffer :register(t3);
StructuredBuffer<uint> particleGridIndexBuffer :register(t4);
StructuredBuffer<Point> points :register(t5);

RWStructuredBuffer<uint2> pointIndexPairs :register(u0);

#define THREADS_PER_GROUP 512

cbuffer Params : register(b0)
{
    float CurrentStep;
    float StepCount;
    float LinesPerSteps;
    // float Threshold;
    // float Dispersion;
    float ParticleGridCellSize;
    // float ClampAccelleration;
    float Time;
    float ScatterLookUp;
    float TestIndex;
}

static const uint            ParticleGridEntryCount = 4;
static const uint            ParticleGridCellCount = 20;
//static const float           ParticleGridCellSize = 0.1f;


bool ParticleGridFind(in float3 position, out uint startIndex, out uint endIndex)
{
    uint i;
    int3 cell = int3(position / ParticleGridCellSize);
    uint cellIndex = (pcg(cell.x + pcg(cell.y + pcg(cell.z))) % ParticleGridCellCount);
    uint hashValue = max(xxhash(cell.x + xxhash(cell.y + xxhash(cell.z))), 1);
    uint cellBegin = cellIndex * ParticleGridEntryCount;
    uint cellEnd = cellBegin + ParticleGridEntryCount;
    for(i = cellBegin; i < cellEnd; ++i)
    {
        const uint entryValue = particleGridHashBuffer[i];
        if(entryValue == hashValue)
            break;  // found existing entry
        if(entryValue == 0)
            i = cellEnd;
    }
    if(i >= cellEnd)
        return false;
    startIndex = particleGridIndexBuffer[i];
    endIndex = particleGridCountBuffer[i] + startIndex;
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
    uint pointCount, stride;
    points.GetDimensions(pointCount, stride);

    uint pairCount;
    pointIndexPairs.GetDimensions(pairCount, stride);


    //float3 position = 0;
    if(TestIndex >= 0) {
        //pointIndexPairs[DTid.x] = uint2( 0, 0);


        int pointIndex = (int)TestIndex;
        float3 position = points[pointIndex].position;
        uint startIndex, endIndex;
        if(ParticleGridFind(position, startIndex, endIndex)) 
        {
            int x2=0;
            for(uint i=startIndex; i < endIndex; ++i) 
            {
                uint otherIndex = particleGridBuffer[i];
                pointIndexPairs[DTid.x + x2] = uint2( pointIndex, otherIndex);
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

    //uint pointIndex =  (indexWithingStep + CurrentStep) % pointCount;

    if(pairIndex >= pairCount)
        return;
    
    // const uint2 gridCell = particleGridCellBuffer[pointIndex];
    // const uint cellIndex = gridCell.x;
    // const uint gridEntryIndex =  gridCell.y;

    // const uint rangeStartIndex = particleGridIndexBuffer[cellIndex];
    // const uint rangeLength = particleGridCountBuffer[cellIndex];
    // const uint endIndex = rangeStartIndex + rangeLength;
    // for(uint i=rangeStartIndex; i < endIndex; ++i) 
    // {
    //     uint point2Index = particleGridBuffer[i];
    //     //points[pointIndex].w = 1;

    //     pointIndexPairs[pairIndex] = int2( pointIndex, point2Index);
    // }

    // if(pointIndex != 10) {
    //      pointIndexPairs[pairIndex] = int2( 4, pointIndex);
    //      return;
    // }
        

    float3 position = points[pointIndex].position;
    float3 jitter = (hash33u( uint3(DTid.x, DTid.x + 134775813U, DTid.x + 1664525U) + position * 100 + Time % 123.1 ) -0.5f)  * ParticleGridCellSize;
    position+= jitter;

    uint startIndex, endIndex;
    if(ParticleGridFind(position, startIndex, endIndex)) 
    {
        const uint particleCount = endIndex - startIndex;
        endIndex = max(startIndex + 64 , endIndex);

        float minDistance = 100000;
        uint closestIndex = -1;

        for(uint i=startIndex ; i < endIndex; ++i) 
        {
            uint otherIndex = particleGridBuffer[i];
            
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