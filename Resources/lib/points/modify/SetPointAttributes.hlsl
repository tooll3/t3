#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{    
    float4 Color;
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
    p.Color = lerp(p.Color, Color, Amount);

    ResultPoints[index] = p; 
}

