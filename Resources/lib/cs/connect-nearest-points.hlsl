#include "point.hlsl"
#include "hash-functions.hlsl"

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
}

static const uint            ParticleGridEntryCount = 32;
static const uint            ParticleGridCellCount = 30000;
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
        
    uint indexWithingStep = DTid.x;
    if(indexWithingStep >= LinesPerSteps)
        return;

    uint stepIndex = CurrentStep % StepCount;

    uint pairIndex = stepIndex * (int)(LinesPerSteps + 0.5) + indexWithingStep;

    uint shuffle = hash11(CurrentStep * 0.123 - indexWithingStep ) * ScatterLookUp * pointCount;

    uint pointIndex = (CurrentStep + shuffle + indexWithingStep ) % pointCount;

    if(pairIndex >= pairCount)
        return;

    float3 position = points[pointIndex].position;
    float3 jitter = (hash33u( uint3(DTid.x, DTid.x + 134775813U, DTid.x + 1664525U) + position * 100 + Time % 123.1 ) -0.5f)  * ParticleGridCellSize;
    position+= jitter;

    uint startIndex, endIndex;
    if(ParticleGridFind(position, startIndex, endIndex)) 
    {
        const uint particleCount = endIndex - startIndex;
        endIndex = max(startIndex + 32 , endIndex);

        float minDistance = 0.5;
        uint closestIndex = -1;

        for(uint i=startIndex; i < endIndex; ++i) 
        {
            uint otherIndex = particleGridBuffer[i];
            if( otherIndex == DTid.x 
            || otherIndex < DTid.x
            )
                continue;

            float3 otherPos = points[otherIndex].position;

            float3 direction = position - otherPos;
            float distance = length(position - otherPos);
            
            if(distance < minDistance)
            {
                minDistance = distance;
                closestIndex = otherIndex;
                break;
            }
        }

        if(closestIndex != -1) {

            pointIndexPairs[pairIndex] = uint2( pointIndex, closestIndex);
            //pointIndexPairs[pairIndex] = uint2( pointIndex, pointIndex+5);
        }
        else {
            pointIndexPairs[pairIndex] = int2( 0, 0);
        }
    }
}