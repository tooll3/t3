//-------------------------------------------------------------------
// BUILD GRID
#include "point.hlsl"
#include "hash-functions.hlsl"
//#include "lib/cs/spatial-grid-functions.hlsl"

// layout(std430) buffer   DispatchCommandBuffer { DispatchCommand dispatchCommandBuffer[]; };
// layout(std430) buffer   AliveIndexBuffer      { uint            aliveIndexBuffer[];      };
// layout(std430) buffer   AliveIndexCountBuffer { uint            aliveIndexCountBuffer[]; };
// layout(std430) buffer   PositionBuffer        { vec4            positionBuffer[];        };

StructuredBuffer<Point> points :register(t0);

RWStructuredBuffer<uint> particleGridBuffer :register(u0);
RWStructuredBuffer<uint2> particleGridCellBuffer :register(u1);
RWStructuredBuffer<uint> particleGridHashBuffer :register(u2);
RWStructuredBuffer<uint> particleGridCountBuffer :register(u3);
RWStructuredBuffer<uint> particleGridIndexBuffer :register(u4);

//uniform uint GroupSize;

#define THREADS_PER_GROUP 512

//StructuredBuffer<uint> particleGridHashBuffer :register(t0);
//StructuredBuffer<uint> particleGridCountBuffer :register(t1);

cbuffer Params : register(b0)
{
    float ParticleGridCellSize;
}

//-------------------------------------------------------------------------
static const uint            ParticleGridEntryCount = 32;
static const uint            ParticleGridCellCount = 30000;
//static const float           ParticleGridCellSize = 0.1f;


bool ParticleGridInsert(in uint index, in float3 position)
{
    uint i;
    int3 cell = int3(position / ParticleGridCellSize);
    uint cellIndex = (pcg(cell.x + pcg(cell.y + pcg(cell.z))) % ParticleGridCellCount);
    uint hashValue = max(xxhash(cell.x + xxhash(cell.y + xxhash(cell.z))), 1);
    uint cellBegin = cellIndex * ParticleGridEntryCount;
    uint cellEnd = cellBegin + ParticleGridEntryCount;
    for(i = cellBegin; i < cellEnd; ++i)
    {
        uint entryValue;
        InterlockedCompareExchange(particleGridHashBuffer[i], 0, hashValue, entryValue);
        if(entryValue == 0 || entryValue == hashValue)
            break;  // found an available entry
    }
    if(i >= cellEnd)
        return false;   // out of memory

    //const uint particleOffset = atomicAdd(particleGridCountBuffer[i], 1);

    uint particleOffset = 0;        
    InterlockedAdd(particleGridCountBuffer[i], 1, particleOffset);
    

    particleGridCellBuffer[index] = uint2(i, particleOffset);
    return true;
}

bool ParticleGridFind(in float3 position, out uint2 entry)
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
    entry.x = particleGridIndexBuffer[i];
    entry.y = particleGridCountBuffer[i] + entry.x;
    return true;
}



//----------------------------------------------------------------------

[numthreads( THREADS_PER_GROUP, 1, 1 )]
void ClearParticleGrid(uint DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    particleGridHashBuffer[DTid.x] = 0;
    particleGridCountBuffer[DTid.x] = 0;
}



[numthreads( THREADS_PER_GROUP, 1, 1 )]
void CountParticlesPerCell(uint DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    uint pointCount, stride;
    points.GetDimensions(pointCount, stride);
    
    if(GI >= pointCount)
        return; // out of bounds

    //const uint particleIndex = aliveIndexBuffer[DTid.x];
    const float3 position = points[DTid.x].position;

    if(!ParticleGridInsert(DTid.x, position))
        particleGridCellBuffer[DTid.x] = uint2(uint(-1), 0);
}

[numthreads( THREADS_PER_GROUP, 1, 1 )]
void ScatterParticlesInCells(uint DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    uint pointCount, stride;
    points.GetDimensions(pointCount, stride);
        
    if(DTid.x >= pointCount)
        return; // out of bounds

    const uint2 gridCell = particleGridCellBuffer[DTid.x];
    if(gridCell.x == uint(-1))
        return; // out of memory

    //const uint particleIndex = aliveIndexBuffer[DTid.x];
    const uint particleOffset = particleGridIndexBuffer[gridCell.x] + gridCell.y;
    particleGridBuffer[particleOffset] = DTid.x;
}