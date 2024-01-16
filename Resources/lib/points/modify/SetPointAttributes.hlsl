#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{    
    float SetPosition;
    float3 Position;

    float SetRotation;
    float3 RotationAxis;

    float RotationAngle;
    float2 __padding;
    float SetStretch;

    float3 Stretch;
    float SetW;

    float W;
    float2 Padding;
    float SetColor;

    float4 Color;

    float SetSelected;
    float Selected;
    float Amount;
}

StructuredBuffer<Point> SourcePoints : t0;        

RWStructuredBuffer<Point> ResultPoints : u0;


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint index = (uint)i.x;
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    if(index >= pointCount) {        
        return;
    }

    Point p = SourcePoints[index];

    if(SetColor > 0.5)
        p.Color = lerp(p.Color, Color, Amount);

    if(SetPosition)
        p.Position = lerp(p.Position, Position, Amount);

    if(SetStretch)
        p.Stretch = lerp(p.Stretch, Stretch, Amount);

    if(SetW)
        p.W = lerp(p.W, W, Amount);

    if(SetRotation) 
    {
        p.Rotation = qSlerp(p.Rotation, qFromAngleAxis(RotationAngle / 180 * PI, RotationAxis), Amount);
    }

    ResultPoints[index] = p; 
}

