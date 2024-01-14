// Texture2D<float> InputTexture : register(t0);
// RWTexture2D<float> summedRows : register (u0);

//Texture2D InputSummedRows : register (t1);
RWTexture2D<float> SummedColumn : register (u0);

[numthreads(1, 1, 1)]
void SumColumns(uint3 threadID : SV_DispatchThreadID)
{
    float sum = 0;
    uint width, height;
    SummedColumn.GetDimensions(width, height);

    // first get sum of column
    for (uint y = 0; y < height; ++y)
    {
        sum += SummedColumn[uint2(width - 1, y)].r;
    }
    SummedColumn[int2(0, height + 1)] = sum;
    
    float sumReciproc = (sum == 0.0f) ? 0 : 1.0f/sum;
    float summedUp = 0; 
    // now sum up, scale by overall sum and store
    for (uint y2 = 0; y2 < height; ++y2)
    {
        summedUp += SummedColumn[uint2(width - 1, y2)].r*sumReciproc;
        SummedColumn[int2(0, y2)].r = summedUp ;
        //SummedColumn[int2(0, y2)] = 1.0f;
    }
    SummedColumn[int2(0, height)] = 1;
    
}
