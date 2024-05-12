#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/hash-functions.hlsl"

cbuffer Params : register(b0)
{
    int ImageWidth;
    int ImageHeight;
}

cbuffer EmitParameter : register(b1)
{
    float Seed;
}

int NumSamples;
Texture2D<float> CDF : register(t0);
Texture2D<float4> Image : register(t1);
RWStructuredBuffer<Point> ResultPoints : u0;

sampler texSampler : register(s0);

[numthreads(256, 1, 1)] void GeneratePoints(uint3 threadID : SV_DispatchThreadID)
{
    int RowSumIndex = ImageWidth;
    int ColumnSumIndex = ImageHeight;

    float2 probability = float2(
        hash11u(((threadID.x + (int)Seed) * _PRIME0) * _PRIME0),
        hash11u((threadID.x + 13 + (int)Seed * _PRIME1) * _PRIME0));

    // use probability.x to find pos in cdf column
    uint index = ImageHeight / 2;
    uint left = 0;
    uint right = ImageHeight;
    uint steps = log2(ImageHeight) + 1;
    for (uint j = 0; j < steps; ++j)
    {
        uint middle = left + (right - left) / 2;
        float cdfSegStart = CDF[uint2(RowSumIndex, middle)];
        float cdfSegEnd = CDF[uint2(RowSumIndex, middle + 1)];
        if (!((probability.x >= cdfSegStart) && (probability.x <= cdfSegEnd)))
        {
            if (probability.x < cdfSegStart)
            {
                right = middle;
            }
            else
            {
                left = middle + 1;
            }
        }
        else
        {
            index = middle;
        }
    }
    uint rowIndex = index;

    // now search cdf row for x index
    left = 0;
    right = ImageWidth;
    steps = log2(ImageWidth) + 0;
    for (uint i = 0; i < steps; ++i)
    {
        uint middle = left + (right - left) / 2;
        index = middle;
        float cdfSegStart = CDF[uint2(middle, rowIndex)];
        float cdfSegEnd = CDF[uint2(middle + 1, rowIndex)];
        if (!((probability.y >= cdfSegStart) && (probability.y <= cdfSegEnd)))
        {
            if (probability.y < cdfSegStart)
            {
                right = middle;
            }
            else
            {
                left = middle + 1;
            }
        }
        else
        {
            index = middle;
        }
    }
    uint columnIndex = index;

    float2 samplePosInUV = float2(float(columnIndex + 0.5) / (ImageWidth - 1),
                                  (1 - float(rowIndex + 0.5) / (ImageHeight - 1)));

    float aspectRatio = (float)ImageHeight / ImageWidth;
    ResultPoints[threadID.x].Color = Image.SampleLevel(texSampler, samplePosInUV * float2(1, -1) + float2(0, 1), 0);
    float2 scatter = float2(hash11u(threadID.x), hash11u(threadID.x * _PRIME0));

    float2 posXY = (samplePosInUV - 0.5) * 2 * float2(1 / aspectRatio, 1) + (scatter - 0.5) * 2 / float2(ImageWidth, ImageHeight);
    ResultPoints[threadID.x].Position = float3(posXY, 0);
    ResultPoints[threadID.x].W = 1;
    ResultPoints[threadID.x].Rotation = float4(0, 0, 0, 1);
    ResultPoints[threadID.x].Selected = 1;
    ResultPoints[threadID.x].Stretch = 1;
}
