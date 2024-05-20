#include "lib/shared/bias-functions.hlsl"

cbuffer Params : register(b0)
{
    int ImageWidth;
    int ImageHeight;
    int Seed;
}

cbuffer Params : register(b1)
{
    float2 BiasAndGain;
    float ScatterWithinPixel;
    float __padding;
    float4 ColorWeight;
}

Texture2D<float4> InputTexture : register(t0);
RWTexture2D<float> CDF : register(u0);

inline float ComputeIntensity(float4 rgba)
{
    // float4 weighted = rgba.rgba * ColorWeight;
    //  float l1 = saturate((rgba.r + rgba.g + rgba.b) / 3) * rgba.a;
    //  float l1 = saturate((weighted.r + weighted.g + weighted.b) / (ColorWeight.r + ColorWeight.g + ColorWeight.b + 0.001)) * rgba.a;
    float l1 = saturate(1.2 - distance(rgba.rgb, ColorWeight.rgb));
    float l = ApplyBiasAndGain(l1, BiasAndGain.x, BiasAndGain.y);
    return l;
}

[numthreads(4, 1, 1)] void SumRows(uint3 threadID : SV_DispatchThreadID)
{
    uint rowIndex = threadID.x;

    if (threadID.y >= ImageHeight)
        return;

    int RowSumIndex = ImageWidth;
    int ColumnSumIndex = ImageHeight;

    float sum = 0;

    // First get sum of row
    for (uint x = 0; x < ImageWidth; ++x)
    {

        float4 rgba = InputTexture[uint2(x, rowIndex)];
        // float l = (rgba.r + rgba.g + rgba.b) * rgba.a;
        sum += ComputeIntensity(rgba);
    }

    CDF[uint2(RowSumIndex, rowIndex)] = sum;
    float sumReciproc = (sum == 0.0f) ? 0 : 1.0f / sum;

    // Now sum up, scale by overall sum and store
    sum = 0;
    for (x = 0; x < ImageWidth; ++x)
    {
        float4 rgba = InputTexture[uint2(x, rowIndex)];
        // float l = (rgba.r + rgba.g + rgba.b) * rgba.a * sumReciproc;
        sum += ComputeIntensity(rgba) * sumReciproc;
        CDF[uint2(x, rowIndex)].r = sum;
    }
}
