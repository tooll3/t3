#include "point.hlsl"

cbuffer Params : register(b0)
{
    float CountA;
    float CountB;
    float ApplyTargetOrietnation;
    float ApplyTargetScaleW;
    float MultiplyTargetW;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
StructuredBuffer<Point> Points2 : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint sourcePointsBatch = (uint)(CountA + 0.1);  
    uint pointIndex = i.x % sourcePointsBatch;
    
    if(pointIndex == sourcePointsBatch-1) {
        ResultPoints[i.x].position =  0;
        ResultPoints[i.x].w = sqrt(-1);
    }
    else {
        uint targetIndex = (i.x / sourcePointsBatch )  % (uint)CountB;
        Point A = SourcePoints[pointIndex];
        Point B = Points2[targetIndex];
        float s = ApplyTargetScaleW > 0.5 ? B.w : 1;
        float3  pLocal = ApplyTargetOrietnation  > 0.5
                        ? rotate_vector(A.position, B.rotation)
                        : A.position;

        ResultPoints[i.x].position = pLocal  * s + B.position;
        ResultPoints[i.x].w = MultiplyTargetW > 0.5 ? A.w * B.w : A.w;
        ResultPoints[i.x].rotation = ApplyTargetOrietnation  > 0.5 ? qmul(B.rotation, A.rotation)
                                                                : A.rotation;
    }

}
