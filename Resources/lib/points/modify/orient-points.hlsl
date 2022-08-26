#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

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
    ResultPoints[i.x].position = p.position;
    ResultPoints[i.x].w = p.w;

    float weight = UseWAsWeight > 0.5   
         ? p.w
         : 1;

    weight*= Amount;


    float sign = Flip > 0.5 ? -1 : 1;
    float4 newRot = q_look_at( normalize(Center - p.position) * sign, normalize(UpVector));

    float3 forward = rotate_vector(float3(0,0,1), newRot);
    float4 alignment= rotate_angle_axis(3.141578, forward);
    newRot = qmul(alignment, newRot);

    ResultPoints[i.x].rotation = normalize(q_slerp(normalize(p.rotation), normalize(newRot), weight));
    

    return;


}

