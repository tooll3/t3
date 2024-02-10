#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer TimeConstants : register(b0)
{
    float GlobalTime;
    float Time;
    float RunTime;
    float BeatTime;
    float LastFrameDuration;
}; 
 

cbuffer Params : register(b1)
{
    float3 GridStretch;
    float CellSize;
    float3 GridOffset;
    float SnapFraction;
    float BlendFraction;
    float UseWAsWeight;
}


float3 fmod(float3 x, float3 y) {
    return (x - y * floor(x / y));
} 

float3 f2(float3 x, float3 y) {
    return ( y * floor(x / y));
} 



StructuredBuffer<Point> Points1 : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    Points1.GetDimensions(pointCount, stride);

    if(i.x >= pointCount) {        
        return;
    }

    Point p = Points1[i.x];

    //ResultPoints[i.x].position = (int3)(Points1[i.x].position / CellSize) * CellSize;
    float3 gridSize = CellSize * GridStretch;
    float3 orgPosition = p.Position;
    float3 pos = orgPosition + gridSize /2 - GridOffset;

    float3 snapPosition = f2(pos, gridSize);
    float3 fraction = abs((pos-snapPosition- gridSize/2) / gridSize)*2;
    float frac1 = (fraction.x + fraction.y + fraction.z);
    frac1 = 1-max(max(fraction.x , fraction.y), fraction.z);
    
    snapPosition += GridOffset;
    
    float weight = UseWAsWeight > 0.5 ? p.W : 1;

    float fractionFactor = saturate(frac1 / BlendFraction + SnapFraction); 
    weight *= fractionFactor;
    snapPosition = lerp(orgPosition, snapPosition,  weight);

    p.Position = snapPosition;
    p.W = frac1;

    ResultPoints[i.x] = p;
}

