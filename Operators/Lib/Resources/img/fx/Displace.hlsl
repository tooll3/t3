// #include "shared/hash-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float DisplaceAmount;
    float DisplaceOffset;
    float Twist;
    float Shade;

    float SampleCount;
    float SampleRadius;
    float SampleSpread;
    float SampleOffset;

    float2 DisplaceMapOffset;
    float DisplaceMode;
    float UseRGSSMultiSampling;
}

cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

cbuffer Resolution : register(b2)
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
Texture2D<float4> DisplaceMap : register(t1);
sampler texSampler : register(s0);

float IsBetween(float value, float low, float high)
{
    return (value >= low && value <= high) ? 1 : 0;
}

static int samples = 1;
static float displaceMapWidth;
static float displaceMapHeight;

float4 DoDisplace(float2 uv)
{
    float4 cx1, cx2, cy1, cy2;
    cx1 = cx2 = cy2 = cy1 = float4(0, 0, 0, 0);
    int dSamples = 2;
    float radius2 = 2;
    float sx = SampleRadius / displaceMapWidth;
    float sy = SampleRadius / displaceMapHeight;
    int sampleIndex = 1;

    float padding = 1;
    float paddingSum;
    float2 d = 0;
    float len = 0;

    float2 direction = 0;
    if (DisplaceMode < 1.5)
    {

        if (DisplaceMode < 0.5)
        {
            for (sampleIndex = 1; sampleIndex < dSamples; sampleIndex++)
            {
                float4 cx1 = DisplaceMap.Sample(texSampler, float2(uv.x + sx * sampleIndex, uv.y) + DisplaceMapOffset) * padding;
                float x1 = (cx1.r + cx1.g + cx1.b) / 3;
                float4 cx2 = DisplaceMap.Sample(texSampler, float2(uv.x - sx * sampleIndex, uv.y) + DisplaceMapOffset) * padding;
                float x2 = (cx2.r + cx2.g + cx2.b) / 3;
                float4 cy1 = DisplaceMap.Sample(texSampler, float2(uv.x, uv.y + sy * sampleIndex) + DisplaceMapOffset) * padding;
                float y1 = (cy1.r + cy1.g + cy1.b) / 3;
                float4 cy2 = DisplaceMap.Sample(texSampler, float2(uv.x, uv.y - sy * sampleIndex) + DisplaceMapOffset) * padding;
                float y2 = (cy2.r + cy2.g + cy2.b) / 3;
                d += float2((x1 - x2), (y1 - y2));

                paddingSum += padding;
                padding /= 1.5;
            }
        }
        else
        {
            float4 rgba = DisplaceMap.Sample(texSampler, uv + DisplaceMapOffset) * padding;
            d = float2(0.0, (rgba.r + rgba.g + rgba.b) / 3.0) / 10;
        }
        float a = (d.x == 0 && d.y == 0)
                      ? 0
                      : (atan2(d.x, d.y) + Twist / 180 * 3.14158);

        direction = float2(sin(a), cos(a));
        len = length(d) + 0.0001;
    }
    else
    {
        float4 rgba = DisplaceMap.Sample(texSampler, uv + DisplaceMapOffset) * padding;
        d = DisplaceMode < 0.5 ? (rgba.rg - 0.5) * 0.01
                               : rgba.rg * 0.01;
        len = length(d) + 0.000001;

        float rRad = Twist / 180 * 3.14158;
        float sina = sin(-rRad);
        float cosa = cos(-rRad);
        d = float2(
            cosa * d.x - sina * d.y,
            cosa * d.y + sina * d.x);

        direction = d / len;
    }

    float2 p2 = direction * (-DisplaceAmount * len * 10 + DisplaceOffset); // * float2(height/ height, 1);
    float imgAspect = TargetWidth / TargetHeight;
    p2.x /= imgAspect;

    float4 t1 = float4(0, 0, 0, 0);
    for (float i = -0.5; i < 0.5; i += 1.0001 / samples)
    {
        t1 += Image.Sample(texSampler, uv + p2 * (i * SampleSpread + 1 - SampleOffset));
    }

    float4 c2 = t1 / samples;
    c2.rgb *= (1 - len * Shade * 100);
    c2.a = clamp(c2.a, 0.00001, 1);
    return c2;
}

float4 psMain(vsOutput psInput) : SV_TARGET
{

    // int samples = (int)clamp(SampleCount + 0.5, 1, 32);
    // float displaceMapWidth, displaceMapHeight;
    DisplaceMap.GetDimensions(displaceMapWidth, displaceMapHeight);

    float2 uv = psInput.texCoord;

    if (UseRGSSMultiSampling > 0.5)
    {
        // 4x rotated grid
        float4 offsets[2];
        offsets[0] = float4(-0.375, 0.125, 0.125, 0.375);
        offsets[1] = float4(0.375, -0.125, -0.125, -0.375);

        float2 sxy = float2(TargetWidth, TargetHeight);

        return (DoDisplace(uv + offsets[0].xy / sxy) +
                DoDisplace(uv + offsets[0].zw / sxy) +
                DoDisplace(uv + offsets[1].xy / sxy) +
                DoDisplace(uv + offsets[1].zw / sxy)) /
               4;
    }
    else
    {
        return DoDisplace(uv);
    }
}