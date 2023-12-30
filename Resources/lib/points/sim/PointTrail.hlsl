#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float TrailLength;
    float AddSeparatorThreshold;
}

cbuffer Params2 : register(b1)
{
    int CycleIndex;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
RWStructuredBuffer<Point> TrailPoints : u0;        // output



[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);

    uint sourceIndex = i.x;
    if(i.x >= pointCount)
        return;

    uint trailLength = (uint)(TrailLength + 0.5);
    uint bufferLength = (uint)(pointCount + 0.5) * trailLength;
    uint cycleIndex = (uint)CycleIndex;
    uint targetIndex = (cycleIndex + sourceIndex * trailLength) % bufferLength;

    TrailPoints[targetIndex] = SourcePoints[sourceIndex];

    float3 lastPos = TrailPoints[(targetIndex-1) % bufferLength ].Position;
    float3 pos = SourcePoints[sourceIndex].Position;
    if(AddSeparatorThreshold > 0) {
        if( length(lastPos - pos) > 0.5) 
            TrailPoints[targetIndex].W = sqrt(-1);
    }

    //TrailPoints[targetIndex].rotation = normalize(qLookAt(SourcePoints[sourceIndex].position, lastPos));

    //Point p = SourcePoints[i.x];
    //TrailPoints[targetIndex].w = 0.4;

    // Flag follow position W as NaN line seperator
    TrailPoints[(targetIndex + 1) % bufferLength].W = sqrt(-1);

    // // Flag too small w as separator
    // if(TrailPoints[targetIndex].w < 0.001 ) 
    // {
    //     TrailPoints[targetIndex].w = sqrt(-1);
    // }
}
