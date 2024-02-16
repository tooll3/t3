#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float AddSeparatorThreshold;
}

cbuffer Params : register(b1)
{
    int CycleIndex;
    int TrailLength;
}

StructuredBuffer<Point> CyclePoints : t0;         // input
RWStructuredBuffer<Point> TrailPoints : u0;        // output

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    CyclePoints.GetDimensions(pointCount, stride);

    if(i.x >= pointCount)
        return;

    int targetIndex = i.x;
    int sourceIndex =   (i.x + CycleIndex +1) % pointCount;

    int bufferLength = pointCount * TrailLength;

    float fInBuffer = (targetIndex % TrailLength ) / (float)(TrailLength-1);

    TrailPoints[pointCount-targetIndex-1] = CyclePoints[sourceIndex];
    
    // Todo: This should be an optional parameter
    if(fInBuffer <= 0)
         fInBuffer = NAN;
         
    if(AddSeparatorThreshold > 0) 
    {
        int sourceIndexLast =   (i.x + CycleIndex +2) % pointCount ;
        float3 lastPos = CyclePoints[sourceIndexLast].Position;
        float3 pos = CyclePoints[sourceIndex].Position;
        if( length(lastPos - pos) > AddSeparatorThreshold)  
        {
            TrailPoints[pointCount-targetIndex-1].Position.z+= 0.1;
            fInBuffer = NAN;
        }
            //TrailPoints[pointCount-targetIndex-1].W = 10; //sqrt(-1);
    }

    TrailPoints[pointCount-targetIndex-1].W = fInBuffer;
}
