#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 TangentDirection;

    float InitWTo01;
    float SegmentCount;

    float TangentA;
    float TangentA_WFactor;
    float TangentB;
    float TangentB_WFactor;
}


StructuredBuffer<Point> PointsA : t0;         // input
StructuredBuffer<Point> PointsB : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)] 
void main(uint3 i : SV_DispatchThreadID)
{
    uint resultCount,  countA, countB, stride;
    ResultPoints.GetDimensions(resultCount, stride);
    PointsA.GetDimensions(countA, stride);
    PointsB.GetDimensions(countB, stride);

    if(i.x >= resultCount)
        return;

    int segmentCount = (int)(SegmentCount+0.5);

    int pointsPerSegment = segmentCount + 1;
 
    uint pairIndex = i.x / pointsPerSegment;
    uint indexInLine = i.x % pointsPerSegment;
    float f = (float)indexInLine / (float)(pointsPerSegment-2);

    //f = f*f;

    if( indexInLine == pointsPerSegment -1) 
    {
        ResultPoints[i.x].w = sqrt(-1); // NaN for divider
        return;
    }

    uint indexA = pairIndex % countA;
    uint indexB = pairIndex % countB;


    float3 pA1 = PointsA[indexA].position;
    float3 pB1 = PointsB[indexB].position;
    float3 forward = TangentDirection;

    float3 tA = rotate_vector( forward, PointsA[indexA].rotation) * (TangentA + PointsA[indexA].w * TangentA_WFactor);
    float3 tB = rotate_vector( forward, PointsB[indexB].rotation) * (TangentB + PointsB[indexB].w * TangentB_WFactor);

    float3 pAA = lerp(pA1, pA1+tA, 1-(1-f)* (1-f));
    float3 pBB = lerp(pB1, pB1+tB,  1-(f*f));

    ResultPoints[i.x].position = lerp(
                                    pAA, 
                                    pBB,
                                    f);
    
    
    ResultPoints[i.x].rotation = float4(1,0,0,1);

    //ResultPoints[i.x].position = float3(1,0,0);
    ResultPoints[i.x].w = 1;
    //ResultPoints[i.x] = PointsA[0];

    // if(InitWTo01 > 0.5)
    //     ResultPoints[i.x].w = f;
}
