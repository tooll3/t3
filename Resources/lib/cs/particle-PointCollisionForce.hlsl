#include "lib/shared/point.hlsl"
 
RWStructuredBuffer<Particle> particles :register(u0);

cbuffer Params : register(b0)
{
    float CellSize;
    float Bounciness;
    float Attraction;
    float SpeedFactor;
    float AttractionDecay;
}

#include "lib/points/spatial-hash-map/spatial-hash-map-lookup.hlsl"

static const float3 CellOffsets[] = 
{
    float3(0,0,0) - 0.5,
    float3(0,0,1) - 0.5,
    float3(0,1,0) - 0.5,
    float3(0,1,1) - 0.5,
    float3(1,0,0) - 0.5,
    float3(1,0,1) - 0.5,
    float3(1,1,0) - 0.5,
    float3(1,1,1) - 0.5,
};


[numthreads( 16, 1, 1 )]
void DispersePoints(uint3 DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    uint gi = DTid.x;

    uint pointCount, stride;
    particles.GetDimensions(pointCount, stride);
        
    if(gi >= pointCount)
        return; // out of bounds
    
    Point p = particles[gi].p;

    float3 position = p.position;
    //float3 searchPos = position; 

    uint startIndex, endIndex;

    int closestIndex = -1;
    float3 closestDirection = 0;
    float closestDistance = 9999999; 

    // float4 rot;
    // float v = q_separate_v(p.rotation, rot);
    // float3 orgV = rotate_vector(float3(0,0,1), rot) * v;

    float3 forceSum =0;
    int count = 0;

    float3 pos = particles[gi].p.position;
    float3 velocity = particles[gi].velocity;
    float3 posNext = pos + velocity * SpeedFactor * 0.01;
    float r = particles[gi].p.w;

    for(int cellOffsetIndex =0; cellOffsetIndex < 8; cellOffsetIndex++) 
    {
        float3 cellOffset = CellOffsets[cellOffsetIndex] * CellSize;

        if(GridFind(position + cellOffset, startIndex, endIndex)) 
        {
            const uint particleCount = endIndex - startIndex;
            float3 sumForces = 0;

            endIndex = min(startIndex + 64 , endIndex);

            for(uint i=startIndex; i < endIndex; ++i) 
            {
                uint otherIndex = CellPointIndices[i];

                if( otherIndex == gi)
                    continue;

                float3 otherPos = particles[otherIndex].p.position;
                float r2 = particles[otherIndex].p.w;

                float3 pToO = pos - otherPos;
                float centerDistance = length(pToO);
                if(centerDistance < 0.0001) 
                {
                    pToO = hash41u(gi).xyz * float3(1,1,0) * 0.01;
                    centerDistance = length(pToO);
                }
                float3 direction = pToO / centerDistance;

                float gap = centerDistance - r2 - r;

                float gapNext = length(posNext - otherPos) - r2 - r;

                //float3 t1 = abs(posInVolume);
                //float gap = max(max(t1.x, t1.y), t1.z);

                // float3 t2 = abs(posInVolumeNext);
                // distanceNext = max(max(t2.x, t2.y), t2.z) - rUnitSphere;
                // collisition
                // if(gi <= otherIndex) 
                //     continue;

                float massRatio = (r) / (r2);
                if(sign( gap * gapNext) < 0  && gap > 0) 
                {
                    //particles[gi].p.w = 10;
                    velocity = reflect(velocity, direction );
                    particles[gi].velocity = velocity * Bounciness;
                    //return;
                }  
                else 
                {
                    // already inside...
                    if(gap < 0) 
                    {
                        particles[gi].p.position -= direction * gap * 1;
                        //particles[gi].velocity -= direction * gap * 10;
                        //return;
                        //particles[gi].p.w = 1;
                        //force = surfaceInWorld * Repulsion;
                    }
                    // Attraction?
                    else 
                    {           
                        particles[gi].velocity -= (direction * Attraction  / massRatio) * (1/( pow(centerDistance, AttractionDecay))) ;
                    }
                    //velocity += force;
                }   
                // float3 direction = position - otherPos;
                // // forceSum += direction;
                // float distance = length(direction);
                // direction /= distance;

                // float wA = p.w;
                // float wB = particles[otherIndex].p.w;
                // float massRatio = (wB * wB) / (wA * wA);
                // //float massRatio = (wB) / (wA);
                // float gapDistance = distance - wA - wB;
                // // float f = gapDistance < 0 ?  -gapDistance*100*Bounciness 
                // //                           :   -clamp( Attraction * smoothstep(0,1, gapDistance * 1) / (1+ gapDistance), -1, 1);
                // if(gapDistance > 0.01)
                //     continue;

                // forceSum += direction * gapDistance*10*Bounciness * massRatio;
                // count++;
            }
        }
    }


    // if(count > 0) 
    // {
    //     //forceSum *= 0.1 / p.w;
    //     //forceSum += orgV;

    //     float forceMag =  clamp(length(forceSum),0, 1000);
    //     float3 forceDirection =  forceSum / (forceMag + 0.0001);
    //     particles[gi].velocity += forceDirection * 0.2;

    //     //particles[gi].p.w = forceMag * 100 + 0.2;
    //     // float angle = atan2(forceSum.x, forceSum.y);
    //     // float4 newRot = rotate_angle_axis(angle, -float3(0,0,1));
    //     // newRot = qmul(newRot, rotate_angle_axis(-PI/2, float3(1,0,0)));
    //     // particles[DTid.x].p.rotation = q_encode_v(newRot, forceMag);
    //     return;
    // }
} 