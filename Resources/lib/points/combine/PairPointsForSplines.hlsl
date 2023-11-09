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

StructuredBuffer<Point> PointsA : t0;        // input
StructuredBuffer<Point> PointsB : t1;        // input
RWStructuredBuffer<Point> ResultPoints : u0; // output

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint resultCount, countA, countB, stride;
    ResultPoints.GetDimensions(resultCount, stride);
    PointsA.GetDimensions(countA, stride);
    PointsB.GetDimensions(countB, stride);

    // if (i.x >= resultCount)
    //     return;

    int segmentCount = (int)(SegmentCount + 0.5);

    int pointsPerSegment = segmentCount ;

    uint pairIndex = i.x / pointsPerSegment;
    uint indexInLine = i.x % pointsPerSegment;
    float f = (float)indexInLine / (float)(pointsPerSegment - 2);

    if (indexInLine == pointsPerSegment - 1)
    {
        ResultPoints[i.x].w = sqrt(-1); // NaN for divider
        return;
    }

    uint indexA = pairIndex % countA;
    uint indexB = pairIndex % countB;

    float3 pA1 = PointsA[indexA].position;
    float3 pB1 = PointsB[indexB].position;
    float3 forward = TangentDirection;

    float paW = PointsA[indexA].w;
    float pbW = PointsA[indexB].w;
    float3 tA = rotate_vector(forward, PointsA[indexA].rotation) * (TangentA + paW * TangentA_WFactor);
    float3 tB = rotate_vector(forward, PointsB[indexB].rotation) * (TangentB + pbW * TangentB_WFactor);

    float3 v0 = pA1;
    float3 v1 = pA1 + tA;
    float3 v2 = pB1 + tB;
    float3 v3 = pB1;

    float t = f;
    float t2 = t * t;
    float t3 = t2 * t;

    float3 pF = (2 * t3 - 3 * t2 + 1) * v0 +
                (t3 - 2 * t2 + t) * tA +
                (-2 * t3 + 3 * t2) * v3 +
                (t3 - t2) * -tB;
    ResultPoints[i.x].position = pF;

    ResultPoints[i.x].rotation = float4(1, 0, 0, 1);


    float w = isnan(paW) || isnan(paW) ? sqrt(-1) : 1;
    ResultPoints[i.x].w = InitWTo01 > 0.5 ? t : w;
    // ResultPoints[i.x] = PointsA[0];

    // if(InitWTo01 > 0.5)
    //     ResultPoints[i.x].w = f;
}
