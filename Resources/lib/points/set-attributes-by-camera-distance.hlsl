#include "point.hlsl"

static const float4 Factors[] = 
{
  //     x  y  z  w
  float4(0, 0, 0, 0), // 0 nothing
  float4(1, 0, 0, 0), // 1 for x
  float4(0, 1, 0, 0), // 2 for y
  float4(0, 0, 1, 0), // 3 for z
  float4(0, 0, 0, 1), // 4 for w
  float4(0, 0, 0, 0), // avoid rotation effects
};

cbuffer Transforms : register(b0)
{
    float4x4 CameraToClipSpace;
    float4x4 ClipSpaceToCamera;
    float4x4 WorldToCamera;
    float4x4 CameraToWorld;
    float4x4 WorldToClipSpace;
    float4x4 ClipSpaceToWorld;
    float4x4 ObjectToWorld;
    float4x4 WorldToObject;
    float4x4 ObjectToCamera;
    float4x4 ObjectToClipSpace;
};

cbuffer Params : register(b1)
{
    float NearRange;
    float FarRange;
}



StructuredBuffer<Point> SourcePoints : t0;
RWStructuredBuffer<Point> ResultPoints : u0;    // output


Texture2D<float4> inputTexture : register(t1);
sampler texSampler : register(s0);

[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    ResultPoints[i.x].rotation = SourcePoints[i.x].rotation;
    float3 pos = SourcePoints[i.x].position;
    ResultPoints[i.x].position = pos;


    float4 distanceFromCamera = mul(float4(pos, 1), ObjectToCamera);
    float d = distanceFromCamera.z;
    
    float normalized = (-d - NearRange) / (FarRange-NearRange);
    float4 t = inputTexture.SampleLevel(texSampler, float2(normalized, 0.5), 0); 

    ResultPoints[i.x].w = SourcePoints[i.x].w * t.r * 1;
}