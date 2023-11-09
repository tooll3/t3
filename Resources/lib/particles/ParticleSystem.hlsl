#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float TriggerEmit;    
    float AgingRate;
    float MaxAge; 
    float Reset;

    float Speed; 
    float Drag;
    float InitialVelocity;
    float Time;

    float OrientTowardsVelocity;
}


cbuffer IntParams : register(b1)
{
    int CollectCycleIndex;
    int WMode;
    int EmitMode;
} 

StructuredBuffer<Point> EmitPoints : t0;
RWStructuredBuffer<Particle> Particles : u0;
RWStructuredBuffer<Point> ResultPoints : u1;


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint newPointCount, pointStride;
    EmitPoints.GetDimensions(newPointCount, pointStride);

    uint maxParticleCount, pointStride2;
    Particles.GetDimensions(maxParticleCount, pointStride2);

    uint gi = i.x;
    if(gi >= maxParticleCount)
        return;

    if(Reset > 0.5)
    {
        Particles[gi].birthTime = NAN;
        Particles[gi].p.position =  NAN;
    }

    // Insert emit points
    int addIndex = 0;
    if(EmitMode == 0) {
        addIndex = (gi + CollectCycleIndex + maxParticleCount) % maxParticleCount;
    } 
    else {
        int t = (gi + CollectCycleIndex / newPointCount) % maxParticleCount;
        int blockSize = maxParticleCount / newPointCount;
        int particleBlock = t / blockSize;
        int t2 = t - (particleBlock * blockSize);
        addIndex =  t2 > 0 ?  -1 : particleBlock;
    }

    if( TriggerEmit > 0.5 && addIndex >= 0 && addIndex < (int)newPointCount )
    {
        if(EmitMode != 0) {
            Particles[(gi-1) % maxParticleCount].birthTime = NAN;
            Particles[(gi-1) % maxParticleCount].p.w = NAN;
        }
        Particles[gi].p = EmitPoints[addIndex];
        Particles[gi].birthTime = Time;
        Particles[gi].velocity = rotate_vector(float3(0,0,1), normalize(Particles[gi].p.rotation)) * InitialVelocity;
    }

    if(Particles[gi].birthTime == NAN)
        return;

    float3 velocity = Particles[gi].velocity;
    velocity *= (1-Drag);
    Particles[gi].velocity = velocity;
    float speed = length(velocity);

    float3 pos = Particles[gi].p.position;
    pos += velocity * Speed * 0.01;
    Particles[gi].p.position = pos;

    if(speed > 0.0001) 
    {
        float f = saturate(speed * OrientTowardsVelocity);
        Particles[gi].p.rotation =  q_slerp(Particles[gi].p.rotation, q_look_at(velocity / speed, float3(0,1,0)),  f );
    }

    // Copy result
    ResultPoints[gi] = Particles[gi].p;

    // Attempt with lerping to smooth position updates
    // ResultPoints[gi].position = lerp(Particles[gi].p.position, ResultPoints[gi].position, 0);
    // ResultPoints[gi].rotation = Particles[gi].p.rotation;
    // ResultPoints[gi].w = Particles[gi].p.w;
    
    if(WMode == 1) 
    {
        ResultPoints[gi].w = isnan(Particles[gi].birthTime) 
                                ?  NAN
                                : clamp((Time - Particles[gi].birthTime) * AgingRate,0, MaxAge);
    } 
    else if(WMode == 2) 
    {
        ResultPoints[gi].w = speed * AgingRate;
    }
}
