#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Center;
    float MaxAcceleration;
    float Acceleration;
    float ApplyMovement;
}

RWStructuredBuffer<Point> Points : u0; 


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    Points.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        Points[i.x].w = 0 ;
        return;
    }

    float3 pos = Points[i.x].position;

    float3 velocity = rotate_vector(float3(0,0, Points[i.x].w), Points[i.x].rotation);

    float3 direction = pos-Center;
    float distance = length(direction);

    //float f= 1- saturate( distance / Radius);
    // if(force < 0.0001)
    //     return;

    float force = clamp( Acceleration/ (distance * distance), 0, MaxAcceleration);
    float3 newV = velocity - normalize(direction) * force;

    float3 up = float3(0,1,0);// cross(normalize(direction), normalize(velocity));
    float4 newOrientation = normalize(q_look_at( normalize(newV), up));
    Points[i.x].w = length(newV);

    //float4 newOrientation = q_look_at( normalize(direction), float3(0,1,0));
    Points[i.x].rotation = newOrientation;
    Points[i.x].position += newV * ApplyMovement ;
}

