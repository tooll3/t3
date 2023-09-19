#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float ConnectPointsMode;
    float ApplyTargetOrientation;
    float ApplyTargetScaleW;
    float MultiplyTargetW;
    float Scale;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
StructuredBuffer<Point> TargetPoints : t1;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

//static const float NAN = sqrt(-1);

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint resultPointCount, sourcePointCount, targetPointCount, stride;
    
    SourcePoints.GetDimensions(sourcePointCount,stride);
    TargetPoints.GetDimensions(targetPointCount,stride);
    ResultPoints.GetDimensions(resultPointCount,stride);

    if(i.x >= resultPointCount) {
        return;
    }

    if(ConnectPointsMode < 0.5) 
    {
        uint sourceLength = sourcePointCount + 1;

        uint sourceIndex = i.x % (sourceLength);
        uint targetIndex = (i.x / sourceLength )  % targetPointCount;
        
        if(sourceIndex == sourcePointCount) {
            ResultPoints[i.x].position =  0;
            ResultPoints[i.x].w = NAN;            
        }
        else {
            Point A = SourcePoints[sourceIndex];
            Point B = TargetPoints[targetIndex];
            float4 rotA = normalize(A.rotation);
            float4 rotB = normalize(B.rotation);

            float s = ApplyTargetScaleW > 0.5 ? B.w : 1;
            s *= Scale;
            float3  pLocal = ApplyTargetOrientation  > 0.5
                            ? rotate_vector(A.position, rotB)
                            : A.position;

            ResultPoints[i.x].position = pLocal  * s + B.position;
            ResultPoints[i.x].w = MultiplyTargetW > 0.5 ? A.w * B.w : A.w;
            ResultPoints[i.x].rotation = ApplyTargetOrientation  > 0.5 ? qmul(rotB, rotA)
                                                                    : rotA;
        }
    }
    else {
        uint loopLength = targetPointCount + 1;
        uint sourceIndex = i.x / loopLength;
        uint targetIndex = i.x % loopLength;
        
        if(targetIndex == loopLength - 1) {
            ResultPoints[i.x].position =  0;
            ResultPoints[i.x].w = NAN;
        }
        else {
            Point sourceP = SourcePoints[sourceIndex];
            Point targetP = TargetPoints[targetIndex];

            float4 sourceRot = normalize(sourceP.rotation);
            float4 targetRot = normalize(targetP.rotation);

            float s = ApplyTargetScaleW > 0.5 ? targetP.w : 1;
            s *= Scale;
            
            float3  pLocal = ApplyTargetOrientation  > 0.5
                            ? rotate_vector(sourceP.position, targetP.rotation)
                            : sourceP.position;

            ResultPoints[i.x].position = pLocal  * s  + targetP.position;
            ResultPoints[i.x].w = MultiplyTargetW > 0.5 ? sourceP.w * targetP.w : sourceP.w;
            ResultPoints[i.x].rotation = ApplyTargetOrientation  > 0.5 ? qmul(targetRot, sourceRot)
                                                                    : sourceRot;
        }

    }

}
