#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float StartIndex;
    float Scatter;
    float Seed;
    float StepSize;
}

StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint sourceCount, stride;
    SourcePoints.GetDimensions(sourceCount, stride);

    uint resultCount, stride2;
    ResultPoints.GetDimensions(resultCount, stride2);

    if(i.x >= resultCount) {        
        return;
    }

    uint scatterOffset = Scatter > 0.001 
                ? (float)sourceCount * Scatter * hash11(i.x+ Seed%4321 + StartIndex%1234)
                : 0;

    uint index = ((uint)StartIndex + (i.x * StepSize) + scatterOffset + 0.1) % sourceCount;
    ResultPoints[i.x] = SourcePoints[index];
}

