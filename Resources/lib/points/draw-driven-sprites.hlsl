#include "point.hlsl"

static const float3 Corners[] = 
{
  float3(-1, -1, 0),
  float3(1, -1, 0), 
  float3(1,  1, 0), 
  float3(1,  1, 0), 
  float3(-1,  1, 0), 
  float3(-1, -1, 0),  
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


cbuffer Params : register(b2)
{
    float4 Color;
    float2 Stretch;
    float2 Offset;
    float Size;
    float WMappingScale;
    float __padding;
    float Rotate;

    float3 RotateAxis;
    float ApplyPointOrientaiton;

};

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
};

sampler texSampler : register(s0);

StructuredBuffer<Point> Points : t0;
Texture2D<float4> SpriteTexture : register(t1);
Texture2D<float4> ColorOverW : register(t2);
Texture2D<float> SizeOverW : register(t3);

psInput vsMain(uint id: SV_VertexID)
{
    psInput output;
    float discardFactor = 1;
    int quadIndex = id % 6;
    int particleId = id / 6;
    float3 cornerFactors = Corners[quadIndex];

    Point p = Points[particleId];

    float3 axis = cornerFactors;
    axis.xy = (axis.xy + Offset) * Stretch;
    axis.z = 0;

    float4 pRotation = ApplyPointOrientaiton > 0.5 ? p.rotation : float4(0,0,0,1);
    
    float4 rotation = qmul(pRotation, rotate_angle_axis(Rotate/180*PI, RotateAxis));
    float sizeFromW = isnan(p.w) ? 0 : SizeOverW.SampleLevel(texSampler, float2(p.w * WMappingScale,0), 0);


    axis = rotate_vector(axis, rotation) * Size * sizeFromW;
    float3 pInObject = p.position + axis;
    output.position  = mul(float4(pInObject,1), ObjectToClipSpace);
    output.texCoord = cornerFactors.xy /2 +0.5;
    output.color = Color * ColorOverW.SampleLevel(texSampler, float2(p.w * WMappingScale,0), 0);    
    return output;    
}

float4 psMain(psInput input) : SV_TARGET
{
    float4 imgColor = SpriteTexture.Sample(texSampler, input.texCoord);
    float4 color = input.color * imgColor;

    return clamp(float4(color.rgb, color.a), 0, float4(1,100,100,100));
}
