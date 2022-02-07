#include "point.hlsl"
#include "hash-functions.hlsl"

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
    float UseWForSize;
    float __padding;
    float Rotate;

    float3 RotateAxis;
    float TextureCellsX;
    float TextureCellsY;
};

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
};

sampler texSampler : register(s0);

StructuredBuffer<Point> Points : t0;
Texture2D<float4> texture2 : register(t1);

psInput vsMain(uint id: SV_VertexID)
{
    psInput output;
    float discardFactor = 1;
    int quadIndex = id % 6;
    int particleId = id / 6;
    float3 cornerFactors = Corners[quadIndex];

    Point p = Points[particleId];

    float3 axis = cornerFactors;

    float2 atlasResolution = 1./float2(TextureCellsX, TextureCellsY);
    float atlasRatio = (float)TextureCellsX/TextureCellsY;

    axis.xy = (axis.xy + Offset) * Stretch * float2(1,atlasRatio);
    axis.z = 0;

    float4 rotation = qmul( normalize(p.rotation), rotate_angle_axis((Rotate + 180)/180*PI , RotateAxis));


    axis = rotate_vector(axis, rotation) * Size * lerp(1,p.w, UseWForSize);
    float3 pInObject = p.position + axis;
    output.position  = mul(float4(pInObject,1), ObjectToClipSpace);
    
    output.texCoord = cornerFactors.xy /2 +0.5;

    int randomParticleId = (int)(hash11((particleId + 13.2) * 123.17) * 12345.3);

    float textureCelX = (float)randomParticleId % (TextureCellsX);
    float textureCelY = (int)(((float)randomParticleId / TextureCellsX) % (float)(TextureCellsY));

    
    //output.texCoord = float2(textureCelX, textureCelY) * 1;
    output.texCoord *= atlasResolution;
    output.texCoord += atlasResolution * float2(textureCelX, textureCelY);

    output.color = Color;
    return output;    
}

float4 psMain(psInput input) : SV_TARGET
{
    //return float4(input.texCoord, 0,1);
    float4 imgColor = texture2.Sample(texSampler, input.texCoord);
    float4 color = input.color * imgColor;

    return clamp(float4(color.rgb, color.a), 0, float4(100,100,100,1));
}
