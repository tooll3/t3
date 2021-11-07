#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float Drag;
    float Speed; 
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


    float3 forward =  normalize(rotate_vector(float3(0,0, 1), Points[i.x].rotation));
    forward *= Points[i.x].w * 0.01 * Speed;
    Points[i.x].position += forward;

    Points[i.x].w *= (1-Drag);

}
