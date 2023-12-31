#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float3 Center;    
    float Amount;
    float3 UpVector;
    float UseWAsWeight;
    float Flip;
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

    Point p = SourcePoints[i.x];

    p.Position = p.Position;
    p.W = p.W;

    float weight = UseWAsWeight > 0.5   
         ? p.W
         : 1;

    weight*= Amount;


    float sign = Flip > 0.5 ? -1 : 1;
    float4 newRot = qLookAt( normalize(Center - p.Position) * sign, normalize(UpVector));

    float3 forward = qRotateVec3(float3(0,0,1), newRot);
    float4 alignment= qFromAngleAxis(3.141578, forward);
    newRot = qMul(alignment, newRot);
    p.Rotation = normalize(qSlerp(normalize(p.Rotation), normalize(newRot), weight));

    ResultPoints[i.x] = p;
}

