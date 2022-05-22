/* An update version of the boid system using a spatial hash map */

#include "hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer ParamConstants : register(b0)
{
    float EffectLayer;
    float GridCellSize;
}


struct Boid
{
    float CohesionRadius;
    float CohesionDrive;
    float AlignmentRadius;
    float AlignmentDrive;
    float SeparationRadius;
    float SeparationDrive;
    float MaxSpeed;
    float _padding;
};

struct Agent {
    float3 Position;
    float BoidType;
    float4 SpriteOrientation;
};

static const float3 Offsets[] = 
{
  float3(0, 0, 0),
  float3(1, 0, 0),
  float3(0, 1, 0), 
  float3(1, 1, 0), 
};


#define mod(x,y) ((x)-(y)*floor((x)/(y)))

sampler texSampler : register(s0);
Texture2D<float4> InputTexture : register(t0);

StructuredBuffer<uint> CellPointIndices :register(t1);     // IndexToPointBuffer -> CellPointIndices
StructuredBuffer<uint2> PointCellIndices :register(t2);    // CellIndicesBuffer -> PointCellIndices
StructuredBuffer<uint> HashGridCells :register(t3);        // HashGridBuffer -> HashGridCells
StructuredBuffer<uint> CellPointCounts :register(t4);      // CountBuffer -> CellPointCounts
StructuredBuffer<uint> CellRangeIndices :register(t5);     // RangeIndexBuffer -> CellRangeIndices
 
RWStructuredBuffer<Boid> BoidsTypes : register(u0);
RWStructuredBuffer<Agent> Agents : register(u1);

static const float3 FORWARD = float3(0,1,0);
static const float3 UP = float3(0,0,1);

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
    int count = min(CellPointCounts[i], 65);

    endIndex = startIndex + count;
    return true;
} 


[numthreads(1024,1,1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint Gi : SV_GroupIndex)
{
    uint pointCount, stride;
    Agents.GetDimensions(pointCount, stride);
        
    if(DTid.x >= pointCount)
        return; // out of bounds


    // Setup Buffers
    float3 centerForCohesion;
    int countForCohesion =0;

    float3 centerForSeparation;
    int countForSeparation =0;

    float3 averageDirection;
    int countForAlignment =0;

    int pointIndex = DTid.x;
    Agent self = Agents[pointIndex];


    // Rotate back
    float3 direction = float3(rotate_vector(FORWARD, self.SpriteOrientation).xy, 0); 
    float3 position = float3(self.Position.xy, 0);
    
    int boidTypIndex = 0;

    int startIndex, endIndex;

    float3 lookupPos = position;
    bool foundOne = false;

    for(uint offsetIndex = 0; offsetIndex < 4; offsetIndex++) 
    {
        float3 posInCel = fmod(position, GridCellSize) - GridCellSize / 2;
        float3 sign = posInCel < 0 ? -1 : 1;
        //lookupPos = floor(position) + posInCel + Offsets[offsetIndex] * GridCellSize;
        lookupPos = position +  Offsets[offsetIndex] * GridCellSize * sign;

        if(GridFind(lookupPos, startIndex, endIndex)) 
        {
            for(uint i=startIndex; i < endIndex; ++i) 
            {
                uint otherIndex = CellPointIndices[i];

                float3 otherPos = Agents[otherIndex].Position;
                float distance =  length(otherPos - position);

                if(distance < BoidsTypes[boidTypIndex].AlignmentRadius)
                {
                    averageDirection += rotate_vector(FORWARD, Agents[otherIndex].SpriteOrientation);
                    countForAlignment++;
                }

                if(distance < BoidsTypes[boidTypIndex].CohesionRadius)
                {
                    centerForCohesion += Agents[otherIndex].Position;
                    countForCohesion++;
                }

                if(distance < BoidsTypes[boidTypIndex].SeparationRadius)
                {
                    centerForSeparation += Agents[otherIndex].Position;
                    countForSeparation++;
                }
            }
            foundOne = true;
        }
    }

    if(!foundOne) {
        //return;
    }



    // Aligment
    if(countForAlignment > 0) 
    {
        averageDirection /= countForAlignment;
        float l = length(averageDirection);
        if(l > 0.0001) {
            direction = lerp(direction, averageDirection/l, BoidsTypes[boidTypIndex].AlignmentDrive);
        }
    }

    // Separation
    if(countForSeparation > 0) 
    {
        centerForSeparation /= countForSeparation;        
        float3 toSeparation = position - centerForSeparation;
        float lenToSeparation = length(position - centerForSeparation);
        if(lenToSeparation > 0.0001) {
            direction = lerp(direction, toSeparation / lenToSeparation, BoidsTypes[boidTypIndex].SeparationDrive );
        }
    }

    // Cohesion
    if(countForCohesion > 0) 
    {
        centerForCohesion /= countForCohesion;        
        float3 toCohesion = -(position - centerForCohesion);
        float lenToCohesion = length(position - centerForCohesion);
        if(lenToCohesion > 0.0001) {
            direction = lerp(direction, toCohesion / lenToCohesion, BoidsTypes[boidTypIndex].CohesionDrive );
        }
    }

    // Effect Texture
    float2 uv= (position.xy * 0.5) +0.5;
    uv = float2(uv.x, 1- uv.y);
    float4 c = InputTexture.SampleLevel(texSampler, uv, 0);
    direction.xy -= c.xy * EffectLayer;

    float len = length(direction);
    if(isnan(len) || len == 0) 
    {
         direction = float3(-1,-1,0);
    }
    else 
    {
        direction /= len;
    }
    

    position += direction * BoidsTypes[boidTypIndex].MaxSpeed;
    //position = mod(position + 1, 2) - 1;

    
    float4 rot = Agents[DTid.x].SpriteOrientation;
    
    // Use look at velocity rotation and rotate back into xy plane
    rot = normalize(q_look_at(direction, float3(0,0,1)));
    rot = qmul(rot, rotate_angle_axis(0.5*PI , float3(1,0,0)));

    // 2d-rotation around z
    Agents[DTid.x].SpriteOrientation = rot;
    Agents[DTid.x].Position = position;
}