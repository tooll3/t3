#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

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
    float Distance;
}

// struct Point {
//     float3 Position;
//     float W;
// };

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

    ResultPoints[i.x].position = Points1[i.x].position +  rotate_vector(Direction * Distance, Points1[i.x].rotation);
    ResultPoints[i.x].rotation = Points1[i.x].rotation;
    ResultPoints[i.x].w = Points1[i.x].w;
}

