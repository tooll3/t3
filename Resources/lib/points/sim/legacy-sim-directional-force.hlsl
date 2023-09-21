#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Direction;
    float Amount;
    float RandomAmount;
    float Mode;
}


RWStructuredBuffer<Point> Points : u0; 
RWStructuredBuffer<SimPoint> SimPoints : u1; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, pointStride;
    SimPoints.GetDimensions(pointCount, pointStride);

    // uint collectedPointCount, pointStride2;
    // CollectedPoints.GetDimensions(collectedPointCount, pointStride2);

    uint gi = i.x;
    if(i.x >= pointCount)
        return;

    float3 offset = Direction * Amount * (1 + hash11(i.x) * RandomAmount);

    SimPoints[i.x].Velocity += offset * 0.01;
}

