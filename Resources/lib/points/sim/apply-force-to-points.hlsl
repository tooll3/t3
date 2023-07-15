#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Center;
    float MaxAcceleration;
    float Acceleration;
    float ApplyMovement;
    float Mode;
}

RWStructuredBuffer<Point> Points : u0; 


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    // uint numStructs, stride;
    // Points.GetDimensions(numStructs, stride);
    // if(i.x >= numStructs) 
    //     return;

    float3 pos = Points[i.x].position;
    float4 rot = Points[i.x].rotation;

    float3 direction = pos-Center;
    float distance = length(direction);

    float force = clamp( Acceleration/ (distance * distance), 0, MaxAcceleration);

    if(Mode < 0.5) 
    {
        float3 velocity = rotate_vector(float3(0,0, Points[i.x].w), rot);
        float3 newV = velocity - normalize(direction) * force;

        float3 up = float3(0,1,0); // cross(normalize(direction), normalize(velocity));
        float4 newOrientation = normalize(q_look_at( normalize(newV), up));
        Points[i.x].w = length(newV);

        Points[i.x].rotation = newOrientation;
        Points[i.x].position += newV * ApplyMovement;
        return;
    }


    float4 normalizedRot;
    float v = q_separate_v(rot, normalizedRot);
    float3 forward = rotate_vector(float3(0,0, v), normalizedRot);
    forward -= normalize(direction) * force;

    float newV = length(forward);
    float4 newRotation = q_look_at(normalize(forward), float3(0,0,1));
    Points[i.x].rotation = q_encode_v(newRotation, newV);    

}

