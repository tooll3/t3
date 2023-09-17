#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Direction;
    float Amount;
    float RandomAmount;
    float Mode;
}

struct SimPoint
{
    float3 Velocity;
    float w;
    float4 Test;
};


RWStructuredBuffer<Point> Points : u0; 
RWStructuredBuffer<SimPoint> SimPoints : u1; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, pointStride;
    SimPoints.GetDimensions(pointCount, pointStride);

    // uint collectedPointCount, pointStride2;
    // CollectedPoints.GetDimensions(collectedPointCount, pointStride2);

    uint gi = i.x;
    if(i.x >= pointCount)
        return;

    float3 offset = Direction * Amount * (1 + hash11(i.x) * RandomAmount);

    // if(Mode < 0.5) 
    // {
    //     Points[i.x].position += offset;
    //     return;
    // }

    // float4 rot = Points[i.x].rotation;
    // float4 normalizedRot;



    // float v = q_separate_v(rot, normalizedRot);

    // float3 forward = rotate_vector(float3(0,0,1), normalizedRot) * v;    
    // forward += offset;

    // float newV = length(forward);
    // float4 newRotation = q_look_at(normalize(forward), float3(0,0,1));

    // Points[i.x].rotation = q_encode_v(newRotation, newV);    

    SimPoints[i.x].Velocity += float3(cos(Amount), sin(Amount),0) * 0.01;
    //Points[i.x].w = Amount * 4;
}

