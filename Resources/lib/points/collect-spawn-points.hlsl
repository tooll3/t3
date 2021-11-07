#include "point.hlsl"

cbuffer Params : register(b0)
{
    float CollectCycleIndex;
    float AddNewPoints;
    float Mode;
    float AgingRate;
    float MaxAge;
    float Reset;
}

StructuredBuffer<Point> NewPoints : t0;         // input
RWStructuredBuffer<Point> CollectedPoints : u0;    // output



[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint newPointCount, pointStride;
    NewPoints.GetDimensions(newPointCount, pointStride);

    uint collectedPointCount, pointStride2;
    CollectedPoints.GetDimensions(collectedPointCount, pointStride2);

    uint gi = i.x;
    if(i.x >= collectedPointCount)
        return;

    if(Reset > 0.5) 
    {
        CollectedPoints[gi].w =  sqrt(-1);
        return;
    }

    int spawnIndex = (int)CollectCycleIndex % collectedPointCount;
    int addIndex = gi - CollectCycleIndex;
    if(AddNewPoints > 0.5 && addIndex >= 0 && addIndex < (int)newPointCount ) 
    {
        // uint trailLength = (uint)(TrailLength + 0.5);
        // uint bufferLength = (uint)(PointCount + 0.5) * trailLength;
        // uint cycleIndex = (uint)(CycleIndex + 0.5);
        // uint targetIndex = (cycleIndex + gi * trailLength) % bufferLength;
        CollectedPoints[gi] = NewPoints[addIndex];
        CollectedPoints[gi].w = 0.0001;
    }
    else 
    {
        float age = CollectedPoints[gi].w;
        if(age <= 0) 
        {
            CollectedPoints[gi].w = sqrt(-1); // Flag non-initialized points 
        }
        else if(age < MaxAge)
        {
            CollectedPoints[gi].w = age+  1/60.0 * AgingRate;
        } 
    }


    //float3 lastPos = CollectedPoints[(targetIndex-1) % bufferLength ].position;
    //CollectedPoints[targetIndex].rotation = normalize(q_look_at(NewPoints[gi].position, lastPos));

    //Point p = NewPoints[i.x];
    //CollectedPoints[targetIndex].w = 0.4;

    // Flag follow position W as NaN line devider
    //CollectedPoints[(targetIndex + 1) % bufferLength].w = sqrt(-1);
}
