#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformMatrix;
    float Mode;
}


StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

static const float PointSpace = 0;
static const float ObjectSpace = 1;
static const float WorldSpace = 2;

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        return;
    }

    float w = SourcePoints[i.x].W;
    float3 p = SourcePoints[i.x].Position;
    float4 rot = SourcePoints[i.x].Rotation;

    p = mul(float4(p,1), TransformMatrix).xyz;

    float4 rotYAxis = qFromAngleAxis(p.x, float3(0,1,0));
    float4 rotXAxis = qFromAngleAxis(-p.y, float3(1,0,0));

    if(Mode > 0.5) 
    {
        p = float3(
            p.x,
            p.z * sin(p.y),
            p.z * cos(p.y)
        );
    }

    p = float3(
        p.z * sin(p.x),
        p.y,
        p.z * cos(p.x)
    );


    if(Mode > 0.5) 
    {
        rot = normalize(qMul(rotXAxis, rot));
    }
    rot = normalize(qMul(rotYAxis, rot));

    ResultPoints[i.x].Position = p;
    ResultPoints[i.x].Rotation = rot;
    ResultPoints[i.x].W = w;
}

