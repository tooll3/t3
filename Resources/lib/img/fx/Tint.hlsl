#include "lib/shared/bias-functions.hlsl"

Texture2D<float4> inputTexture : register(t0);
sampler texSampler : register(s0);

cbuffer ParamConstants : register(b0)
{
    float4 MapBlackTo;
    float4 MapWhiteTo;
    float4 ChannelWeights;
    float Amount;
    float2 GainAndBias;
}


cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

// float GetSchlickBias(float x, float bias)
// {
//     float x1 = x * 2;
//     float x2 = x * 2 - 1;
//     float bias1 = 1 - bias;
//     return x < 0.5
//         ? x1 / ((1 / bias - 2) * (1 - x1) + 1) / 2
//         : x2 / ((1 / bias1 - 2) * (1 - x2) + 1) / 2 + 0.5;
// }

float4 psMain(vsOutput psInput) : SV_TARGET
{
    //return float4(1,1,0,1); 
    float2 uv = psInput.texCoord;
    float4 c = inputTexture.SampleLevel(texSampler, uv, 0.0);

    float t = length(c * normalize(ChannelWeights)) + 0.001;
    //float b = Bias +1;
    // t = Bias> 0 
    //     ? pow( t, Bias+1)
    //     : 1-pow( 1-t, -Bias+1);
    //t = GetSchlickBias(t, clamp(Bias, 0.001, 0.999));
    t= ApplyGainBias(t, GainAndBias.x, GainAndBias.y); 
    float4 mapped = lerp(MapBlackTo, MapWhiteTo, t); 
    //return float4(t,0,0,1);
    float4 cout = lerp(c, mapped, Amount);
    cout.a = clamp(cout.a, 0,1);
    return cout;
}
