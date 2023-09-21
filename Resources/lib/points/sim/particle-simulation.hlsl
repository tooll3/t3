#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float EmitEmitPoints;    
    float AgingRate;
    float MaxAge;
    float Reset;

    float Speed; 
    float Drag;

    float SetInitialVelocity;
    float InitialVelocity;
    float Time;
}


cbuffer IntParams : register(b1)
{
    int CollectCycleIndex;
    int WMode;
}

StructuredBuffer<Point> EmitPoints : t0;
RWStructuredBuffer<Point> CollectedPoints : u0;
RWStructuredBuffer<SimPoint> SimPoints : u1; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint newPointCount, pointStride;
    EmitPoints.GetDimensions(newPointCount, pointStride);

    uint collectedPointCount, pointStride2;
    CollectedPoints.GetDimensions(collectedPointCount, pointStride2);

    uint gi = i.x;
    if(gi >= collectedPointCount)
        return;

    if(Reset > 0.5)
    {
        CollectedPoints[gi].w = NAN;
        CollectedPoints[gi].position =  NAN;
        return;
    }

    // Insert emit points
    int addIndex = (gi - CollectCycleIndex + collectedPointCount) % collectedPointCount;
    if( EmitEmitPoints > 0.5 && addIndex >= 0 && addIndex < (int)newPointCount )
    {
        CollectedPoints[gi] = EmitPoints[addIndex];
        SimPoints[gi].BirthTime = Time;
        SimPoints[gi].Velocity = SetInitialVelocity > 0.5 
                                ? rotate_vector(float3(0,0,1), normalize(CollectedPoints[gi].rotation)) * InitialVelocity
                                : 0;
    }

    if(CollectedPoints[gi].w == NAN)
        return;


    // just return original w
    if(WMode == 0) {
    
    }

    // Return age
    else if(WMode == 1) {
        CollectedPoints[gi].w = isnan(CollectedPoints[gi].w) ?  NAN: clamp((Time - SimPoints[gi].BirthTime) * AgingRate,0, MaxAge);
    } 

    // Return speed
    else if(WMode == 2) {
        CollectedPoints[gi].w = length(SimPoints[gi].Velocity);
    }

    // Update other points
    // if(UseAging > 0.5 ) 
    // {
    //     float age = CollectedPoints[gi].w;

    //     if(!isnan(age)) 
    //     {    
    //         if(age <= 0)
    //         {
    //             CollectedPoints[gi].w = sqrt(-1); // Flag non-initialized points
    //         }
    //         else if(age < MaxAge)
    //         {
    //             CollectedPoints[gi].w = age+  DeltaTime * AgingRate;
    //         }
    //         else if(ClampAtMaxAge) {
    //             CollectedPoints[gi].w = MaxAge;
    //         }
    //     }
    // }

    
    //Point p = CollectedPoints[gi];

    float3 velocity = SimPoints[gi].Velocity;
    float3 pos = CollectedPoints[gi].position;
    pos += velocity * Speed * 0.01;// * (int)(DeltaTime * 1000*)/(1000.0*6);
    velocity *= (1-Drag);
    SimPoints[gi].Velocity = velocity;
    CollectedPoints[gi].position = pos;
}
