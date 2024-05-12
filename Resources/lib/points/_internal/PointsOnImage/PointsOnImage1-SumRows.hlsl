cbuffer Params : register(b0)
{
    int ImageWidth;
    int ImageHeight;
    int WidthWithSums;
    int HeightWithSums;
}

Texture2D<float4> InputTexture : register(t0);
RWTexture2D<float> CDF : register(u0);

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
        float l = (rgba.r + rgba.g + rgba.b) * rgba.a;
        sum += l;
    }

    CDF[uint2(RowSumIndex, rowIndex)] = sum;
    float sumReciproc = (sum == 0.0f) ? 0 : 1.0f / sum;

    // Now sum up, scale by overall sum and store
    sum = 0;
    for (x = 0; x < ImageWidth; ++x)
    {
        float4 rgba = InputTexture[uint2(x, rowIndex)];
        float l = (rgba.r + rgba.g + rgba.b) * rgba.a * sumReciproc;
        sum += l;
        CDF[uint2(x, rowIndex)].r = sum;
    }
}
