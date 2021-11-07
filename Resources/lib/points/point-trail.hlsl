#include "point.hlsl"

cbuffer Params : register(b0)
{
    float TrailLength;
    float CycleIndex;
    float HasPointCountChanged;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
RWStructuredBuffer<Point> TrailPoints : u0;    // output



[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    //uint pointCount = (uint)(PointCount + 0.5);
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);

    uint sourceIndex = i.x;
    if(i.x >= pointCount)
        return;

    uint trailLength = (uint)(TrailLength + 0.5);
    uint bufferLength = (uint)(pointCount + 0.5) * trailLength;
    uint cycleIndex = (uint)(CycleIndex + 0.5);
    uint targetIndex = (cycleIndex + sourceIndex * trailLength) % bufferLength;

    TrailPoints[targetIndex] = SourcePoints[sourceIndex];

    //float3 lastPos = TrailPoints[(targetIndex-1) % bufferLength ].position;
    //TrailPoints[targetIndex].rotation = normalize(q_look_at(SourcePoints[sourceIndex].position, lastPos));

    //Point p = SourcePoints[i.x];
    //TrailPoints[targetIndex].w = 0.4;

    // Flag follow position W as NaN line seperator
    TrailPoints[(targetIndex + 1) % bufferLength].w = sqrt(-1);

    // Flag too small w as separator
    if(TrailPoints[targetIndex].w < 0.001 ) 
    {
        TrailPoints[targetIndex].w = sqrt(-1);
    }
}
