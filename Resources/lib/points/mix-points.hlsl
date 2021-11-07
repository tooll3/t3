#include "hash-functions.hlsl"
#include "point.hlsl"


cbuffer Params : register(b0)
{
    float BlendFactor;
    float CountA;
    float CountB;
    float CombineMode;
}


StructuredBuffer<Point> Points1 : t0;         // input
StructuredBuffer<Point> Points2 : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    Point A = Points1[i.x];
    Point B = Points2[i.x];
    ResultPoints[i.x].position =  lerp(A.position, B.position, BlendFactor); 
    ResultPoints[i.x].w = lerp(A.w, B.w, BlendFactor);
}

