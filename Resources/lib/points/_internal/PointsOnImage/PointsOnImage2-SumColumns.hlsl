cbuffer Params : register(b0)
{
    int ImageWidth;
    int ImageHeight;
}

RWTexture2D<float> Sums : register(u0);

[numthreads(1, 1, 1)] void SumColumns(uint3 threadID : SV_DispatchThreadID)
{
    // First get sum of column
    float sum = 0;
    int RowSumIndex = ImageWidth;
    int ColumnSumIndex = ImageHeight;

    for (uint y = 0; y < ImageHeight; ++y)
    {
        sum += Sums[uint2(RowSumIndex, y)];
    }
    // Sums[int2(0, ColumnSumIndex)] = sum;

    float sumReciproc = (sum == 0.0f) ? 0 : 1.0f / sum;
    float normalizedSum = 0;

    // Now sum up, scale by overall sum and store
    for (y = 0; y < ImageHeight; ++y)
    {
        normalizedSum += Sums[uint2(RowSumIndex, y)] * sumReciproc;
        Sums[int2(RowSumIndex, y)].r = saturate(normalizedSum);
    }
    // Sums[int2(0, ImageHeight)] = 1;
}
