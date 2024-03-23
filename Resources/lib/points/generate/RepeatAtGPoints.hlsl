#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float ConnectPointsMode;
    float ApplyTargetOrientation;
    float ApplyTargetScaleW;
    float MultiplyTargetW;

    float Scale;
    float AddSeperators;
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
        bool addSeperators = AddSeperators > 0.5;
        uint sourceLength = sourcePointCount + addSeperators;

        uint sourceIndex = i.x % (sourceLength);
        uint targetIndex = (i.x / sourceLength )  % targetPointCount;
        
        if(addSeperators && sourceIndex == sourcePointCount) 
        {
            ResultPoints[i.x].Position =  0;
            ResultPoints[i.x].W = NAN;            
        }
        else 
        {
            Point A = SourcePoints[sourceIndex];
            Point B = TargetPoints[targetIndex];
            float4 rotA = normalize(A.Rotation);
            float4 rotB = normalize(B.Rotation);

            float s = ApplyTargetScaleW > 0.5 ? B.W : 1;
            s *= Scale;
            float3  pLocal = ApplyTargetOrientation  > 0.5
                            ? qRotateVec3(A.Position, rotB)
                            : A.Position;

            ResultPoints[i.x].Position = pLocal  * s + B.Position;
            ResultPoints[i.x].W = MultiplyTargetW > 0.5 ? A.W * B.W : A.W;
            ResultPoints[i.x].Rotation = ApplyTargetOrientation  > 0.5 ? qMul(rotB, rotA) : rotA;
            ResultPoints[i.x].Color = SourcePoints[sourceIndex].Color * TargetPoints[targetIndex].Color;
            ResultPoints[i.x].Selected = SourcePoints[sourceIndex].Selected * TargetPoints[targetIndex].Selected;
            ResultPoints[i.x].Stretch = SourcePoints[sourceIndex].Stretch * TargetPoints[targetIndex].Stretch;
            

        }
    }
    else {
        uint loopLength = targetPointCount ;
        uint sourceIndex = i.x / loopLength;
        uint targetIndex = i.x % loopLength;
        
        if(targetIndex == loopLength -1) {
            ResultPoints[i.x].Position =  0;
            ResultPoints[i.x].W = NAN;
        }
        else {
            Point sourceP = SourcePoints[sourceIndex];
            Point targetP = TargetPoints[targetIndex];

            float4 sourceRot = normalize(sourceP.Rotation);
            float4 targetRot = normalize(targetP.Rotation);

            float s = ApplyTargetScaleW > 0.5 ? targetP.W : 1;
            s *= Scale;
            
            float3  pLocal = ApplyTargetOrientation  > 0.5
                            ? qRotateVec3(sourceP.Position, targetP.Rotation)
                            : sourceP.Position;

            ResultPoints[i.x].Position = pLocal  * s  + targetP.Position;
            ResultPoints[i.x].W = MultiplyTargetW > 0.5 ? sourceP.W * targetP.W : targetP.W;
            ResultPoints[i.x].Rotation = ApplyTargetOrientation  > 0.5 ? qMul(targetRot, sourceRot) : sourceRot;
            ResultPoints[i.x].Color = SourcePoints[sourceIndex].Color * TargetPoints[targetIndex].Color;
            ResultPoints[i.x].Selected = SourcePoints[sourceIndex].Selected * TargetPoints[targetIndex].Selected;
        }
    }
}
