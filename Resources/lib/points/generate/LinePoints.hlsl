#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"


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
    float OrientationMode;
    
    float4 ColorA;
    float4 ColorB;

    float AddSeparator;
    float2 BiasGain;
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
    if(index > pointCount)  
        return;

    if(AddSeparator > 0.5 && index == pointCount -1) {
        ResultPoints[index].W = sqrt(-1); 
        return;
    }

    int seperatorOffset = AddSeparator > 0.5 ? 1 :0;
    int steps = (pointCount - 1 - seperatorOffset);
    float f1 = GetBiasGain(steps > 0 ? (float)(index)/steps : 0.5, BiasGain.x, BiasGain.y);
    float f =  f1 - Pivot;
    
    ResultPoints[index].Position = lerp(Center, Center + Direction * LengthFactor, f);
    ResultPoints[index].W = W + WOffset * (float)(index)/steps;

    float4 rot2 = 0;
    if(OrientationMode < 0.5) 
    {
        float4 rotate = qFromAngleAxis(3.141578/2 * 1, float3(0,0,1));

        rotate = qMul( rotate, qFromAngleAxis( (OrientationAngle + Twist * f) / 180 * 3.141578, float3(0,1,0)));

        float3 upVector = float3(0,0,1);
        float t = abs(dot( normalize(Direction), normalize(upVector)));
        if(t > 0.999) {
            upVector = float3(0,1,0);
        }
        float4 lookAt = qLookAt(normalize(Direction), upVector);
        
        //rot2 = normalize(qMul(rotate, lookAt));            
        rot2 = normalize(qMul(rotate, lookAt));
    }
    else 
    {
        // FIXME: this rotation is hard to control and feels awkward. 
        // I didn't come up with another method, though
        rot2 = normalize(qFromAngleAxis((OrientationAngle + Twist * f) / 180 * 3.141578, ManualOrientationAxis));
    }

    ResultPoints[index].Stretch = 1;
    ResultPoints[index].Rotation = rot2;
    ResultPoints[index].Color = lerp(ColorA, ColorB, f1);
    ResultPoints[index].Selected = 1;
}

