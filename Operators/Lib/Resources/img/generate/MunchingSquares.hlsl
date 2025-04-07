#include "shared/bias-functions.hlsl"
#include "shared/blend-functions.hlsl"
#include "shared/hash-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float4 Black;
    float4 White;
    float4 GrayScaleWeights;


    float2 GainAndBias;
    float2 Stretch;
    float Scale;
    float Method;

    float2 Offset;
    float BlendMode;
    float Iteration;
    float IterationFx;

    float IsTextureValid;
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

Texture2D<float4> Image : register(t0);
sampler texSampler : register(s0);

#define mod(x, y) ((x) - (y) * floor((x) / (y)))

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float aspectRatio = TargetWidth / TargetHeight;
    float2 p = psInput.texCoord;
    p -= 0.5;

    int round = 1;
    int2 res = int2((int)TargetWidth / round, (int)TargetHeight / round) * round;

    // This will prevent repetitive artifacts in the pattern
    float epsilonScale = Scale - 0.0001f;

    float2 divisions = res / epsilonScale;
    float2 fixOffset = Offset * float2(-1, 1) / divisions;
    p += fixOffset;

    float2 p1 = p;
    float2 gridSize = float2(1 / divisions.x, 1 / divisions.y);
    float2 pInCell = mod(p1, gridSize);
    float2 cellIds = (p1 - pInCell + 0.5);


    float2 cellTiles = cellIds - fixOffset;

    pInCell *= divisions;

    float4 color = Image.Sample(texSampler, cellTiles) * GrayScaleWeights;
    
    float grayScale = (color.r + color.g + color.b)/3;
    float biased = ApplyGainAndBias(grayScale, GainAndBias);

    float fxIterations = Iteration + IterationFx * biased;

    int F = (int)(fxIterations+0.5);
    int X = (int)(cellIds.x * divisions.x / Stretch.x + 0.5);  //  int(@P.x*float(chi("factorX")));
    int Y = (int)(cellIds.y * divisions.y / Stretch.y + 0.5);  //int(@P.z*float(chi("factorY")));
    int blackOrWhite = !(X&F^Y&F);

    float4 c = lerp(Black, White, blackOrWhite);
    return (IsTextureValid < 0.5) ? c : BlendColors(color, c, (int)BlendMode);    
}