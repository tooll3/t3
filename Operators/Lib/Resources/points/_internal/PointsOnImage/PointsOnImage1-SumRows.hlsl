Texture2D<float> InputTexture : register(t0);
RWTexture2D<float> summedRows : register (u0);

[numthreads(1, 8, 1)]
void SumRows(uint3 threadID : SV_DispatchThreadID)
{
    float sum = 0;
    uint width, height;
    InputTexture.GetDimensions(width, height);

    // first get sum of row
    for (uint x = 0; x < width; ++x)
    {
        sum += InputTexture[uint2(x, threadID.y)].r;
    }
    summedRows[uint2(width + 1, threadID.y)] = sum;
    
    float sumReciproc = (sum == 0.0f) ? 0 : 1.0f/sum;

    float summedUp = 0;
    // now sum up, scale by overall sum and store
    for (uint x2 = 0; x2 < width; ++x2)
    {
        summedUp += InputTexture[uint2(x2, threadID.y)].r * sumReciproc;
        summedRows[uint2(x2, threadID.y)].r = summedUp;
    }
    
    summedRows[uint2(width, threadID.y)] =  1.0f;
}
