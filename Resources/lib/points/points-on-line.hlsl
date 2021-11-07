#include "hash-functions.hlsl"
#include "point.hlsl"


cbuffer Params : register(b0)
{
    float3 Center;
    float LengthFactor;

    float3 Direction;
    float Pivot;

    float W;
    float WOffset;
    float OrientationAngle;
    float Twist;

    float3 ManualOrientationAxis;
    float OriationMode;
    
    float AddSeparator;
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
    uint index = i.x; 

    uint pointCount, stride;
    ResultPoints.GetDimensions(pointCount, stride);

    if(AddSeparator > 0.5 && index == pointCount -1) {
        ResultPoints[index].w = sqrt(-1);
        return;
    }

    int seperatorOffset = AddSeparator > 0.5 ? 1 :0;
    float f = (float)(index)/(pointCount-1 - seperatorOffset) - Pivot;

    ResultPoints[index].position = lerp(Center, Center + Direction * LengthFactor, f);
    ResultPoints[index].w = W + WOffset * f;

    // FIXME: this rotation is hard to control and feels awkward. 
    // I didn't come up with another method, though
    float4 rot2 = 0;
    if(OriationMode < 0.5) 
    {
        float4 rotate = rotate_angle_axis(3.141578/2 * 1, float3(0,0,1));

        rotate = qmul( rotate, rotate_angle_axis( (OrientationAngle + Twist * f) / 180 * 3.141578, float3(0,1,0)));

        //float3 axis = OriationMode > 0.5 ? ManualOrientationAxis : Direction;

        float3 upVector = float3(0,0,1);
        float t = abs(dot( normalize(Direction), normalize(upVector)));
        if(t > 0.999) {
            upVector = float3(0,1,0);
        }
        float4 lookAt = q_look_at(normalize(Direction), upVector);

        
        rot2 = normalize(qmul(rotate, lookAt));
    }
    else {
        //float3 axis = OriationMode > 0.5 ? ManualOrientationAxis : Direction;
        rot2 = normalize(rotate_angle_axis(OrientationAngle / 180 * 3.141578, ManualOrientationAxis));
        //rot2 = qmul(q_look_at(Direction, float3(1,1,0)), rotate);
    }

    //float4 rotate = rotate_angle_axis(OrientationAngle / 180 * 3.141578, normalize(Direction));

    ResultPoints[index].rotation = rot2;
}

