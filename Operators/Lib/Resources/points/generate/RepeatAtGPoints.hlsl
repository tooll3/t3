#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Scale;
}

cbuffer Params : register(b1)
{
    int ApplyTargetOrientation;
    int ScaleFactorMode;
    int SetF1To;
    int SetF2To;

    int ConnectPointsMode;
    int AddSeperators;
}

StructuredBuffer<Point> SourcePoints : t0;   // input
StructuredBuffer<Point> TargetPoints : t1;   // input
RWStructuredBuffer<Point> ResultPoints : u0; // output

[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    uint resultPointCount, sourcePointCount, targetPointCount, stride;

    SourcePoints.GetDimensions(sourcePointCount, stride);
    TargetPoints.GetDimensions(targetPointCount, stride);
    ResultPoints.GetDimensions(resultPointCount, stride);

    if (i.x >= resultPointCount)
    {
        return;
    }

    if (ConnectPointsMode == 0)
    {
        bool addSeperators = AddSeperators > 0.5;
        uint sourceLength = sourcePointCount + addSeperators;

        uint sourceIndex = i.x % (sourceLength);
        uint targetIndex = (i.x / sourceLength) % targetPointCount;

        if (addSeperators && sourceIndex == sourcePointCount)
        {
            ResultPoints[i.x].Position = 0;
            ResultPoints[i.x].Scale = NAN;
        }
        else
        {
            Point sourceP = SourcePoints[sourceIndex];
            Point targetP = TargetPoints[targetIndex];
            float4 rotA = normalize(sourceP.Rotation);
            float4 rotB = normalize(targetP.Rotation);

            float s = 1; // ApplyTargetScaleW > 0.5 ? targetP.W : 1;
            s *= Scale;
            float3 pLocal = ApplyTargetOrientation > 0.5
                                ? qRotateVec3(sourceP.Position, rotB)
                                : sourceP.Position;

            ResultPoints[i.x].Position = pLocal * s * targetP.Scale + targetP.Position;
            ResultPoints[i.x].Rotation = ApplyTargetOrientation > 0.5 ? qMul(rotB, rotA) : rotA;
            ResultPoints[i.x].Color = SourcePoints[sourceIndex].Color * TargetPoints[targetIndex].Color;
            ResultPoints[i.x].FX1 = SetF1To ? sourceP.FX1 * targetP.FX1 : sourceP.FX1;
            ResultPoints[i.x].FX2 = SourcePoints[sourceIndex].FX2 * TargetPoints[targetIndex].FX2;
            ResultPoints[i.x].Scale = SourcePoints[sourceIndex].Scale * TargetPoints[targetIndex].Scale;
        }
    }
    else
    {
        uint loopLength = targetPointCount;
        uint sourceIndex = i.x / loopLength;
        uint targetIndex = i.x % loopLength;

        if (targetIndex == loopLength - 1)
        {
            ResultPoints[i.x].Position = 0;
            ResultPoints[i.x].Scale = NAN;
        }
        else
        {
            Point sourceP = SourcePoints[sourceIndex];
            Point targetP = TargetPoints[targetIndex];

            float4 sourceRot = normalize(sourceP.Rotation);
            float4 targetRot = normalize(targetP.Rotation);

            float s = 1; // ApplyTargetScaleW > 0.5 ? targetP.W : 1;
            s *= Scale;

            float3 pLocal = ApplyTargetOrientation > 0.5
                                ? qRotateVec3(sourceP.Position, targetP.Rotation)
                                : sourceP.Position;

            ResultPoints[i.x].Position = pLocal * s + targetP.Position;
            ResultPoints[i.x].Rotation = ApplyTargetOrientation > 0.5 ? qMul(targetRot, sourceRot) : sourceRot;
            ResultPoints[i.x].Color = SourcePoints[sourceIndex].Color * TargetPoints[targetIndex].Color;

            ResultPoints[i.x].FX1 = SetF1To ? sourceP.FX1 * targetP.FX1 : targetP.FX1;
            ResultPoints[i.x].FX2 = SourcePoints[sourceIndex].FX2 * TargetPoints[targetIndex].FX2;
            ResultPoints[i.x].Scale = SourcePoints[sourceIndex].Scale * TargetPoints[targetIndex].Scale;
        }
    }
}
