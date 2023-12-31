#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
cbuffer Params : register(b0)
{
    int startIndex;    
}

StructuredBuffer<Point> Points : t0;            // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(256,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint size, stride;
    Points.GetDimensions(size, stride);

    if(i.x > size)
        return;

    uint targetIndex = i.x + (int)startIndex;
    ResultPoints[targetIndex] = Points[i.x];
}
