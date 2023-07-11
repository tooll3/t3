#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

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
    if(i.x >= numStructs) 
        return;


    Point p = Points[i.x];
    float4 rot;
    float v = q_separate_v(p.rotation, rot);

    float3 forward =  normalize(rotate_vector(float3(0, 0, 1), rot));

    forward *= v * 0.01 * Speed;
    p.position += forward;

    v *= (1-Drag);
    p.rotation = q_encode_v(rot, v);

    Points[i.x] = p;

}
