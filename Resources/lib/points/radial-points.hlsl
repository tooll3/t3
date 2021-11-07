#include "hash-functions.hlsl"
#include "point.hlsl"


cbuffer Params : register(b0)
{
    float Count;
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

}


//StructuredBuffer<Point> Points1 : t0;         // input
//StructuredBuffer<Point> Points2 : t1;         // input
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
//void main(uint i : SV_GroupIndex)
{
    uint index = i.x; 
    bool closeCircle =  CloseCircle > 0.5;
    float count = closeCircle ? (Count -2) : Count;

    float f = (float)(index)/count;
    float l = Radius + RadiusOffset * f;
    float angle = (StartAngle * 3.141578/180 + Cycles * 2 *3.141578 * f);
    float3 up = Axis.y > 0.7 ? float3(0,0,1) :  float3(0,1,0);
    float3 direction = normalize(cross(Axis, up));
    //float direction = normalize(Axis);

    float3 v2 = RotatePointAroundAxis(direction * l , Axis, angle);

    float3 c= Center + CenterOffset * f;
    float3 v =  v2 + c;

    
    ResultPoints[index].position = v;
    ResultPoints[index].w = (closeCircle && index == Count -1)
                          ? sqrt(-1) // NaN
                          : W + WOffset * f;

    //float4 orientation = normalize(rotate_angle_axis(OrientationAngle * 3.141578/180 , normalize(OrientationAxis)));
    float4 orientation = rotate_angle_axis(3.141578/2 * 1, normalize(OrientationAxis));

    orientation = qmul( orientation, rotate_angle_axis( (OrientationAngle) / 180 * 3.141578, float3(1,0,0)));

    float4 lookat = q_look_at(Axis, up);

    float4 quat = qmul(   orientation, rotate_angle_axis(angle, normalize(Axis)));

    float4 spin = rotate_angle_axis( (OrientationAngle) / 180 * 3.141578, normalize(OrientationAxis));
    float4 spin2 = rotate_angle_axis( angle, float3(Axis));

    ResultPoints[index].rotation = qmul(normalize(qmul(spin2, lookat)), spin);
}

