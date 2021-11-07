#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"
#include "utils.hlsl"

cbuffer Params : register(b0)
{
    float3 Translate;
    float ScatterTranslate;

    float3 Scale;
    float ScaleMagnitude;

    float3 RotateAxis;
    float RotateAngle;

    float3 VolumePosition;
    float VolumeType;

    float3 VolumeSize;
    float VolumeScaleMagnitude;

    float SoftRadius;
    float Bias;
    float UseWAsWeight;
}


StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   


float sdEllipsoid( float3 p, float3 r )
{
  float k0 = length(p/r);
  float k1 = length(p/(r*r));
  return k0*(k0-1.0)/k1;
}

float Bias2(float x, float bias)
{
    float biasNormalized = (clamp(Bias, 0.005, 0.995)+1) / 2;
    return x / ((1 / biasNormalized - 2) * (1 - x) + 1);
}

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        ResultPoints[i.x].w = 0 ;
        return;
    }

    Point p = SourcePoints[i.x];

    float3 pToCenter = p.position - VolumePosition;

    float r = length(VolumeSize);
    float d1 = sdEllipsoid(pToCenter, VolumeSize.xyz/2 * VolumeScaleMagnitude);

    float d = smoothstep( 0.5/r + SoftRadius, 0.5/r, d1*2);
    float dBiased = Bias2(d, Bias);
    dBiased *= UseWAsWeight < 0 ? lerp(1, 1- p.w, -UseWAsWeight) 
                                : lerp(1, p.w, UseWAsWeight);

    float4 rotation = rotate_angle_axis(RotateAngle * dBiased * PI/180, normalize(RotateAxis));
    pToCenter = rotate_vector(pToCenter, rotation);

    p.position = lerp(p.position, VolumePosition + pToCenter * Scale * ScaleMagnitude,  dBiased) + dBiased * Translate;
    ResultPoints[i.x] = p;
    ResultPoints[i.x].rotation = qmul(p.rotation, rotation);
}

