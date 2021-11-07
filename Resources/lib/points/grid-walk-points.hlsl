#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float3 GridSize;
    float Speed;

    float3 GridOffset;
    float SpeedVariation;

    float TriggerTurn;
    float Seed;
}

RWStructuredBuffer<Point> ResultPoints : u0; 

static const float4 axisAngles[] = 
{
  float4( 0, 1, 0, 0),
  float4( 0, 1, 0, 1),
  float4( 0, 1, 0, 2),
  float4( 0, 1, 0, 3),
  float4( 1, 0, 0, -1),
  float4( 1, 0, 0, 1),
};


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    ResultPoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        ResultPoints[i.x].w = 0 ;
        return;
    }

    Point p = ResultPoints[i.x];
    float3 forward = float3(0,0,-1);
    float3 velocity = rotate_vector(forward, normalize(p.rotation)) * Speed;

    float3 localVelocity = velocity;

    float3 localPosition = mod(p.position - GridOffset, GridSize);
    float3 newLocalPosition = localPosition + localVelocity;

    float hash = hash11((Seed + i.x + p.position.x + p.position.y + p.position.z) * 421 % 1231);
    if( newLocalPosition.x <= 0 || newLocalPosition.x >= GridSize.x 
     || newLocalPosition.y <= 0 || newLocalPosition.y >= GridSize.y 
     || newLocalPosition.z <= 0 || newLocalPosition.z >= GridSize.z
     || TriggerTurn > 0.5
    ) {
        newLocalPosition = clamp(numStructs,0, GridSize);

        uint r = (uint)(hash * 10) % 4;
        float3 axis = r == 0 ? float3(-1,0,0) 
                                  : r == 1 ? float3(0,-1,0)
                                                : float3(0,0,-1);
        float4 axisAndAngle = axisAngles[(int)(hash * 6) % 6];
        ResultPoints[i.x].rotation = rotate_angle_axis(axisAndAngle.w * PI/1.5, axisAndAngle.xyz);
    }
    ResultPoints[i.x].position += velocity;
}

