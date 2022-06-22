#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

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
    float3 Direction;
    float CellSize;
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
    uint numStructs, stride;
    Points1.GetDimensions(numStructs, stride);

    if(i.x >= numStructs) {
        ResultPoints[i.x].w = 0 ;
        return;
    }

    //ResultPoints[i.x].position = (int3)(Points1[i.x].position / CellSize) * CellSize;
    ResultPoints[i.x].position = f2(Points1[i.x].position, CellSize * Direction);
    ResultPoints[i.x].rotation = Points1[i.x].rotation;
    ResultPoints[i.x].w = Points1[i.x].w;
}

