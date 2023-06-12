#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float AddNewPoints;
    float UseAging;
    float AgingRate;
    float MaxAge;

    float ClampAtMaxAge;
    float Reset;
    float DeltaTime;
}

cbuffer IntParams : register(b1)
{
    int CollectCycleIndex;
}

StructuredBuffer<Point> NewPoints : t0;
RWStructuredBuffer<Point> CollectedPoints : u0;

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

    //int spawnIndex = (int)CollectCycleIndex % collectedPointCount;

    int addIndex = (gi - CollectCycleIndex) % collectedPointCount;

    // Insert emit points
    if(AddNewPoints > 0.5 && addIndex >= 0 && addIndex < (int)newPointCount )
    {
        CollectedPoints[gi] = NewPoints[addIndex];
        if(UseAging > 0.5)
            CollectedPoints[gi].w = 0.0001;
    }
    // Update other points
    else
    {

        float age = CollectedPoints[gi].w;
        if(isnan(age) || UseAging < 0.5)
            return;

        if(age <= 0)
        {
            CollectedPoints[gi].w = sqrt(-1); // Flag non-initialized points
        }
        else if(age < MaxAge)
        {
            CollectedPoints[gi].w = age+  DeltaTime * AgingRate;
        }
        else if(ClampAtMaxAge) {
            CollectedPoints[gi].w = MaxAge;
        }
    }
}
