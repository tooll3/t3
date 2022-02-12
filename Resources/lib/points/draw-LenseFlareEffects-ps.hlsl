#include "point.hlsl"
#include "hash-functions.hlsl"

static const float4 Corners[] = 
{
    //   px py  u v
  float4(-1, -1, 0,1),
  float4( 1, -1, 1,1), 
  float4( 1,  1, 1,0), 
  float4( 1,  1, 1,0), 
  float4(-1,  1, 0,0), 
  float4(-1, -1, 0,1),  
};

cbuffer Params : register(b0)
{
    float Time;

    float NoiseComplexity;
    float DistributionNoise;

    float IntensityNoise;

    float CoreIntensity;
    float Gamma;
    float Colorize;

};

cbuffer Transforms : register(b1)
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

cbuffer Resolution : register(b1)
{
    float TargetWidth;
    float TargetHeight;
}


struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
    float spriteIndex : COLOR1;
};


struct Sprite
{
    float2 PosInClipSpace;
    float2 Size;
    float Rotation;
    float4 Color;
    float2 UvMin;
    float2 UvMax;
    float3 __padding;
};

sampler texSampler : register(s0);

StructuredBuffer<Sprite> Sprites : t0;
Texture2D<float4> NoiseImage : register(t1);

psInput vsMain(uint id: SV_VertexID)
{
    psInput output;
    float discardFactor = 1;
    int cornerIndex = id % 6;
    int particleId = id / 6;
    output.spriteIndex = particleId;
    float4 cornerFactors = Corners[cornerIndex];

    float4 aspect = float4(CameraToClipSpace[1][1] / CameraToClipSpace[0][0],1,1,1);    

    Sprite sprite = Sprites[particleId];

    float2 corner = float2(cornerFactors.x * sprite.Size.x, 
                          cornerFactors.y * sprite.Size.y);


    float imageRotationRad = (-sprite.Rotation - 90) / 180 * PI;     

    float sina = sin(-imageRotationRad - PI/2);
    float cosa = cos(-imageRotationRad - PI/2);

    corner = float2(
        cosa * corner.x - sina * corner.y,
        cosa * corner.y + sina * corner.x 
    );                              

    float2 p = float2( corner.x / aspect.x + sprite.PosInClipSpace.x,
                       corner.y + sprite.PosInClipSpace.y);

    output.position = float4(p, 0,1);

    output.texCoord = lerp(sprite.UvMin, sprite.UvMax, cornerFactors.zw);


    output.color = sprite.Color;
    return output;    
}

float remap(float value, float inMin, float inMax, float outMin, float outMax) {
    float factor = (value - inMin) / (inMax - inMin);
    float v = factor * (outMax - outMin) + outMin;
    return v;
}

float4 psMain(psInput input) : SV_TARGET
{
    float2 p = input.texCoord;
    p -= 0.5;

    float d = length(p) * 2;
    if(d > 1)
        return 0;
 
    float2 Center = 0;

    float2 dir2 = p-Center;
    float ldir2 = length(dir2);
    float angle = (atan2(dir2.x, dir2.y)) / (2*PI)+0.5;

    float2 noisePos = float2(angle * NoiseComplexity +  input.spriteIndex*0.1, 
                             angle*0.2 + Time*0.1 + input.spriteIndex * 0.2);
    float4 noiseA =  NoiseImage.SampleLevel(texSampler, noisePos, 0.0) * 1;

    angle += (noiseA.r - 0.5) * DistributionNoise;

    float4 noiseB =  NoiseImage.SampleLevel(texSampler, float2(
                        angle * NoiseComplexity + input.spriteIndex * 0.2, 
                        angle*0.3 + Time*-0.11 + input.spriteIndex * 0.13
                        ), 0.0);

    float brightness = pow(1-d,3);
    brightness *= lerp(1, (noiseB.b), IntensityNoise);

    brightness +=  pow(1-d,10) * CoreIntensity;

    brightness = pow(brightness, Gamma);
    float4 colorOut = input.color * float4(brightness.xxx * lerp(1,noiseB.rgb, Colorize),1);

    return clamp(float4(colorOut.rgb, colorOut.a), 0, float4(1000,1000,1000,1));
}
