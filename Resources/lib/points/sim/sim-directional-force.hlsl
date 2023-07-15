#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Direction;
    float Amount;
    float RandomAmount;
    float Mode;
}


RWStructuredBuffer<Point> ResultPoints : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{

    float3 offset = Direction * Amount * (1 + hash11(i.x) * RandomAmount);

    if(Mode < 0.5) 
    {
        ResultPoints[i.x].position += offset;
        return;
    }

    float4 rot = ResultPoints[i.x].rotation;
    float4 normalizedRot;

    float v = q_separate_v(rot, normalizedRot);

    float3 forward = rotate_vector(float3(0,0,1), normalizedRot) * v;    
    forward += offset;

    float newV = length(forward);
    float4 newRotation = q_look_at(normalize(forward), float3(0,0,1));

    ResultPoints[i.x].rotation = q_encode_v(newRotation, newV);    
}

