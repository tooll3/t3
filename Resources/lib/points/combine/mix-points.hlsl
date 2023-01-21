#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"


cbuffer Params : register(b0)
{
    float BlendFactor;
    float BlendMode;
    float PairingMode;
}
 

StructuredBuffer<Point> PointsA : t0;         // input
StructuredBuffer<Point> PointsB : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint resultCount, countA, countB, stride;
    ResultPoints.GetDimensions(resultCount, stride);
    PointsA.GetDimensions(countA, stride);
    PointsB.GetDimensions(countB, stride);

    if(i.x > resultCount)
        return;

    uint aIndex = i.x;
    uint bIndex = i.x;


    if(PairingMode > 0.5 && countA != countB) {
        float t = i.x / (float)resultCount;
        aIndex = (int)(countA *t);
        bIndex = (int)(countB *t);
    }
        

    Point A = PointsA[aIndex];
    Point B = PointsB[bIndex];
    
    float f =0;

    if(BlendMode < 0.5) {
        f = BlendFactor;
    }
    else if(BlendMode < 1.5) {
        f = A.w;        
    }
    else if(BlendMode < 2.5) {
        f = (1-B.w);
    }

    ResultPoints[i.x].position =  lerp(A.position, B.position, f); 
    ResultPoints[i.x].w = lerp(A.w, B.w, f);
}

