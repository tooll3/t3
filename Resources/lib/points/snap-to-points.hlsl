#include "hash-functions.hlsl"
#include "point.hlsl"


cbuffer Params : register(b0)
{
    float BlendFactor;
    float Distance;
    float MaxAmount;
}


StructuredBuffer<Point> Points1 : t0;         // input
StructuredBuffer<Point> Points2 : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    Point A = Points1[i.x];
    Point SnapPoint = Points2[i.x];
    float distance = length(A.position - SnapPoint.position);
    float blendFactor = smoothstep( BlendFactor + Distance, Distance  , distance ) * MaxAmount;

    ResultPoints[i.x].position =  lerp(A.position, SnapPoint.position, blendFactor);
    ResultPoints[i.x].w = lerp(A.w, SnapPoint.w, BlendFactor);
}

