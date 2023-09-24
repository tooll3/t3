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
    float OrientTowardsVelocity;
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



    float3 velocity = SimPoints[gi].Velocity;
    velocity *= (1-Drag);
    SimPoints[gi].Velocity = velocity;
    float speed = length(velocity);

    // just return original w
    if(WMode == 0) {
    
    }

    // Return age
    else if(WMode == 1) {
        CollectedPoints[gi].w = isnan(CollectedPoints[gi].w) ?  NAN: clamp((Time - SimPoints[gi].BirthTime) * AgingRate,0, MaxAge);
    } 

    // Return speed
    else if(WMode == 2) {
        //CollectedPoints[gi].w = speed;
        CollectedPoints[gi].w = SimPoints[gi].Velocity.y * 10 ;
    }


    float3 pos = CollectedPoints[gi].position;
    pos += velocity * Speed * 0.01;
    CollectedPoints[gi].position = pos;

    if(speed > 0.0001) 
    {
        float f = saturate(speed * OrientTowardsVelocity);
        CollectedPoints[gi].rotation =  q_slerp(CollectedPoints[gi].rotation, q_look_at(velocity / speed, float3(0,1,0)),  f ) ;
    }
}
