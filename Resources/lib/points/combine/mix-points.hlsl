#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"


cbuffer Params : register(b0)
{
    float BlendFactor;
    float BlendMode;
}


StructuredBuffer<Point> Points1 : t0;         // input
StructuredBuffer<Point> Points2 : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    Point A = Points1[i.x];
    Point B = Points2[i.x];

    float f =BlendFactor;

    if(BlendMode < 1.5) {
        f = A.w;        
    }
    else if(BlendMode < 2.5) {
        f = (1-B.w);
    }

    ResultPoints[i.x].position =  lerp(A.position, B.position, f); 
    ResultPoints[i.x].w = lerp(A.w, B.w, f);
}

