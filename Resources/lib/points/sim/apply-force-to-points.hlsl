#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Center;
    float Radius;
    float Force;
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


    float3 direction = pos-Center;
    float3 v = Points[i.x].w * rotate_vector(float3(0,0,1), Points[i.x].rotation);

    float distanceToCenter = length(direction);

    float f= 1- saturate( distanceToCenter / Radius);
    if(f < 0.001)
        return;

    
    float3 newV = v + (direction/ distanceToCenter) * Force * f;
    float4 newOrientation = q_look_at( normalize(newV), float3(0,1,0));
    Points[i.x].w = length(newV);

    //float4 newOrientation = q_look_at( normalize(direction), float3(0,1,0));
    Points[i.x].rotation = newOrientation;
    Points[i.x].position += newV * 0.01;
}

