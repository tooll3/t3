/*

PatternAmount
PatternStretch
PatternSize
PatternBlurX,Y
PatternBlurAmount

Distort

GlitchTime
GlitchAmount
GlitchVignette
Offset
Flicker
Noise
NoiseColorize

BlurBackdrop
OverdrawSteps
Scale
OffsetX
OffsetY

*/

Texture2D<float4> inputTexture : register(t0);
sampler texSampler : register(s0);

cbuffer ParamConstants : register(b0)
{
    float PatternAmount;
    float PatternAspect;
    float PatternSize;
    float ShiftColums;
    float Gaps;
    float2 PatternOffset;

    float PatternBlurX;
    float PatternBlurY;

    float Buldge;
    float GlitchTime;
    float GlitchAmount;
    float GlitchVignette;
    float GlitchOffset;
    float GlitchFlicker;
    float Noise;
    float NoiseColorize;
    float BlurBackdrop;
    float OverdrawSteps;
    float Scale;
    float OffsetX;
    float OffsetY;

    float BlurImage;

    float P1;
    float P2;
    float P3;
    float P4;
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

#define mod(x, y) ((x) - ((y)*floor((x) / (y))))

static const float3 RgbColors[] = {
    float3(1, 0, 0),
    float3(0, 1, 0),
    float3(0, 0, 1),
};

float GetColor(int rgbIndex, float x, float py, float sourceImageChannel)
{
    x += (rgbIndex - 1) / 3.0;
    float xx = pow((0.5 - abs(x - 0.5) + P3 + sourceImageChannel * P1), P2);
    float s = smoothstep(1 - PatternBlurX, 1 + PatternBlurY, xx);
    return s;
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float2 uv = psInput.texCoord;
    float aspectRatio = TargetWidth / TargetHeight;

    float2 p = uv;
    p -= 0.5;

    // Bulge distort
    p -= p * Buldge * (0.5 - dot(p, p));

    float2 uv2 = p + 0.5;
    float4 imgCol1 = inputTexture.SampleLevel(texSampler, uv2, 0);
    float4 imgColBlurred =
        imgCol1 * 0.2 +
        inputTexture.SampleLevel(texSampler, uv2, 1) * 0.2 +
        inputTexture.SampleLevel(texSampler, uv2, 2) * 0.2 +
        inputTexture.SampleLevel(texSampler, uv2, 3) * 0.2 +
        inputTexture.SampleLevel(texSampler, uv2, 4) * 0.2 +
        inputTexture.SampleLevel(texSampler, uv2, 5) * 1;

    float4 imgCol = lerp(imgCol1, imgColBlurred, BlurImage);
    imgCol.a = saturate(imgCol.a);

    float2 divisions = float2(aspectRatio, 1) * 4 / (PatternSize * float2(PatternAspect, 1));
    float2 pCentered = (p + PatternOffset / divisions * float2(-1, 1));
    float2 pScaled = pCentered * divisions;

    float pInCellX = mod(pScaled.x, 1);
    int cellIdX = pScaled.x - pInCellX;
    float pInCellY = mod(pScaled.y + cellIdX * ShiftColums, 1);
    int cellIdY = pScaled.y - pInCellY;

    float2 pInCell = float2(pInCellX, pInCellY);

    int rgbStripeIndex = int(pInCellX * 3);

    float xInStripe = (pInCellX - 0.5) / (1 - Gaps * 2) + 0.5;

    float3 cc = float3(
        GetColor(0, xInStripe, pInCellY, imgCol.r),
        GetColor(1, xInStripe, pInCellY, imgCol.g),
        GetColor(2, xInStripe, pInCellY, imgCol.b));

    float padding = Gaps;
    float yBlur = smoothstep(padding - PatternBlurY, padding + PatternBlurY, abs(pInCellY - 0.5));

    float4 pattern = float4(cc * yBlur, 1);

    float4 r = lerp(imgCol, pattern, PatternAmount);

    // Vignette
    r.rgb *= -GlitchVignette * (1 - (0.5 - dot(p, p)) - 0.5) + 1.5;

    return float4(r);
}
