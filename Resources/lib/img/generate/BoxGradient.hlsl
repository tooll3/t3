#include "lib/shared/blend-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float2 Size;
    float Width;
    float Offset;
    float PingPong;
    float Repeat;
    float BlendMode;
    float2 BiasAndGain;
    

    float IsTextureValid; // Automatically added by _FxShaderSetup
}

cbuffer Resolution : register(b1)
{
    float TargetWidth;
    float TargetHeight;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> ImageA : register(t0);
Texture2D<float4> Gradient : register(t1);
sampler texSampler : register(s0);
sampler clammpedSampler : register(s1);

float fmod(float x, float y)
{
    return (x - y * floor(x / y));
}

//source: https://iquilezles.org/articles/distfunctions2d/
float sdBox( in float2 p, in float2 b )
{
    float2 d = abs(p)-b;
    return length(max(d,0.0)) + min(max(d.x,d.y),0.0);
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float2 uv = psInput.texCoord;

    float aspectRation = TargetWidth / TargetHeight;
    float2 p = uv;
    p -= 0.5;
    p.x *= aspectRation;
    
    float c = 0;


    c = sdBox(p+Center*float2(-1,1), Size)* 2 - Offset * Width;
 
   
    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);

    c = PingPong > 0.5
            ? (Repeat < 0.5 ? (abs(c) / Width)
                            : 1.000001 - abs(fmod(c, Width * 1.99999) - Width) / Width)
            : c / Width;

    c = Repeat > 0.5
            ? fmod(c, 1)
            : saturate(c);

    float dBiased = ApplyBiasAndGain(c, BiasAndGain.x, BiasAndGain.y);
    // float dBiased = Bias >= 0
    //                     ? pow(c, Bias + 1)
    //                     : 1 - pow(clamp(1 - c, 0, 10), -Bias + 1);

    dBiased = clamp(dBiased, 0.001, 0.999);
    float4 gradient = Gradient.Sample(clammpedSampler, float2(dBiased, 0));

    return (IsTextureValid < 0.5) ? gradient : BlendColors(orgColor, gradient, (int)BlendMode);
}