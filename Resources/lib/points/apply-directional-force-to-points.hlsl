#include "hash-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float3 Direction;
    float Amount;
    float RandomAmount;
}


RWStructuredBuffer<Point> ResultPoints : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    ResultPoints[i.x].position += Direction * Amount * (1 + hash11(i.x) * RandomAmount);
    ResultPoints[i.x].w += 0;
}

