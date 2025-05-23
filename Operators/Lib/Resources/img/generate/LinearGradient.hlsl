#include "shared/blend-functions.hlsl"
#include "shared/bias-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float Width;
    float Rotation;

    float PingPong;
    float Repeat;
    float2 GainAndBias;

    float Offset;
    float SizeMode;
    float BlendMode;
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
sampler clampedSampler : register(s1);

inline float fmod(float x, float y)
{
    return (x - y * floor(x / y));
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float2 uv = psInput.texCoord;

    float aspectRation = TargetWidth / TargetHeight;
    float2 p = uv;
    p -= 0.5;

    if (SizeMode < 0.5)
    {
        p.x *= aspectRation;
    }
    else
    {
        p.y /= aspectRation;
    }

    float radians = Rotation / 180 * 3.141578;
    float2 angle = float2(sin(radians), cos(radians));

    float c = dot(p - Center, angle);
    c += Offset;
    c = PingPong > 0.5
            ? (Repeat < 0.5 ? (abs(c) / Width)
                            //: (1.0000 - abs(fmod(c, Width * 2) - Width) / Width))
                            : (1.0000 - abs(fmod((c + 0.5 / TargetHeight) / Width, 2) - 1)))
            : c / Width + 0.5;

    c = Repeat > 0.5
            ? fmod(c, 1)
            : saturate(c);

    float dBiased = ApplyGainAndBias(saturate(c), GainAndBias);
    // float dBiased = Bias >= 0
    //                     ? pow(c, Bias + 1)
    //                     : 1 - pow(clamp(1 - c, 0, 10), -Bias + 1);

    dBiased = clamp(dBiased, 0.000001, 0.99999);
    // dBiased = c;

    float4 gradient = Gradient.Sample(clampedSampler, float2(dBiased, 0));
    // return gradient;

    if (IsTextureValid < 0.5)
        return gradient;

    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);
    return (IsTextureValid < 0.5) ? gradient : BlendColors(orgColor, gradient, (int)BlendMode);
}