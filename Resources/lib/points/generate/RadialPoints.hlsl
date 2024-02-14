#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"


cbuffer Params : register(b0)
{
    float __padding;
    float Radius;
    float RadiusOffset;
    float __padding1;

    float3 Center;
    float __padding2;

    float3 CenterOffset;
    float __padding3;

    float StartAngle;
    float Cycles;
    float2 __padding4;
    
    float3 Axis;
    float W;

    float WOffset;
    float CloseCircle;    
    float2 __padding5;

    float3 OrientationAxis;
    float1 OrientationAngle;

    float2 GainAndBias;

}


RWStructuredBuffer<Point> ResultPoints : u0;    // output

float3 RotatePointAroundAxis(float3 In, float3 Axis, float Rotation)
{
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    float3x3 rot_mat = 
    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    return mul(rot_mat,  In);
}

[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount,stride;
    ResultPoints.GetDimensions(pointCount, stride);

    if(i.x >= pointCount)
        return;

    uint index = i.x; 
    bool closeCircle =  CloseCircle > 0.5;
    float angleStepCount = closeCircle ? (pointCount -2) : pointCount;

    float ff = (float)(index)/angleStepCount;
    float f = ApplyBiasAndGain(ff, GainAndBias.x, GainAndBias.y);

    float l = Radius + RadiusOffset * f;
    float angle = (StartAngle * 3.141578/180 + Cycles * 2 *3.141578 * f);
    float3 up = Axis.y > 0.7 ? float3(0,0,1) :  float3(0,1,0);
    float3 direction = normalize(cross(Axis, up));

    float3 v2 = RotatePointAroundAxis(direction * l , Axis, angle);

    float3 c= Center + CenterOffset * f;
    float3 v =  v2 + c;

    
    ResultPoints[index].Position = v;
    ResultPoints[index].W = (closeCircle && index == pointCount -1)
                          ? NAN
                          : W + WOffset * f;

    float4 orientation = qFromAngleAxis(3.141578/2 * 1, normalize(OrientationAxis));

    orientation = qMul( orientation, qFromAngleAxis( (OrientationAngle) / 180 * 3.141578, float3(1,0,0)));

    float4 lookat = qLookAt(Axis, up);

    float4 quat = qMul(   orientation, qFromAngleAxis(angle, normalize(Axis)));

    float4 spin = qFromAngleAxis( (OrientationAngle) / 180 * 3.141578, normalize(OrientationAxis));
    float4 spin2 = qFromAngleAxis( angle, float3(Axis));

    ResultPoints[index].Rotation = qMul(normalize(qMul(spin2, lookat)), spin);
    ResultPoints[index].Color = 1;
    ResultPoints[index].Stretch = 1;
    ResultPoints[index].Selected = 1;

}

