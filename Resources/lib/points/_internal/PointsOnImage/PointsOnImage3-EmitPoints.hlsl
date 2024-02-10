#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/hash-functions.hlsl"

cbuffer EmitParameter : register(b0)
{
    float Seed;
};

// float radicalInverse_VdC(uint bits) 
// {
//      bits = (bits << 16u) | (bits >> 16u);
//      bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
//      bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
//      bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
//      bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
//      return float(bits) * 2.3283064365386963e-10; // / 0x100000000
// }


// float2 hammersley2d(uint i, uint N) 
// {
//     return float2(float(i)/float(N), radicalInverse_VdC(i));
// }

uint wang_hash(in out uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

int NumSamples;
Texture2D<float> CDF : register (t0);
Texture2D<float4> Image : register (t1);
RWStructuredBuffer<Point> ResultPoints : u0;

sampler texSampler : register(s0);

[numthreads(256,1,1)]
void GeneratePoints(uint3 threadID : SV_DispatchThreadID)
{
    uint rowWidth, columnHeight;
    //cdfRows.GetDimensions(rowWidth, columnHeight);
    CDF.GetDimensions(rowWidth, columnHeight);
    
    rowWidth -= 2; columnHeight -= 2;
    //float2 prob = hammersley2d(threadID.x, NumSamples);

    uint rng_stateX = (threadID.x * (uint)(Seed * 10317));
    uint rng_stateY = (threadID.x * (uint)(Seed * 1331337));

    // float xi = ;
    // float xi = (float(wang_hash(rng_stateY)) * (1.0 / 4294967296.0));
    float2 prob = float2(
        (float(wang_hash(rng_stateX)) * (1.0 / 4294967296.0)),
        (float(wang_hash(rng_stateY)) * (1.0 / 4294967296.0)));

    // use prob.x to find pos in cdf column
    uint index = columnHeight/2;   
   
    uint left = 0;
    uint right = columnHeight;
    uint steps = log2(columnHeight) + 1;
    for (uint j = 0; j < steps; ++j)
    {
        uint middle = left + (right - left)/2;
        // float cdfSegStart = cdfColumn[uint2(0, middle)].x;
        // float cdfSegEnd = cdfColumn[uint2(0, middle + 1)].x;
        float cdfSegStart = CDF[uint2(0, middle)].x;
        float cdfSegEnd = CDF[uint2(0, middle + 1)].x;
        if (!((prob.x >= cdfSegStart) && (prob.x <= cdfSegEnd)))
        {
            if (prob.x < cdfSegStart)
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
    right = rowWidth;
    steps = log2(rowWidth) + 1;
    for (uint i = 0; i < steps; ++i)
    {
        uint middle = left + (right - left)/2;
        // float cdfSegStart = cdfRows[uint2(middle, rowIndex)].x;
        // float cdfSegEnd = cdfRows[uint2(middle + 1, rowIndex)].x;
        float cdfSegStart = CDF[uint2(middle, rowIndex)].x;
        float cdfSegEnd = CDF[uint2(middle + 1, rowIndex)].x;
        if (!((prob.y >= cdfSegStart) && (prob.y <= cdfSegEnd)))
        {
            if (prob.y < cdfSegStart)
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

    float aspectRatio = (columnHeight - 1.0) / (rowWidth -2);
    //float2 samplePosInUV = float2(float(columnIndex)/float(columnHeight -1), (1-float(rowIndex)/float(rowWidth -2)));
    float2 samplePosInUV = float2(float(columnIndex)/float(rowWidth -1), (1-float(rowIndex)/float(columnHeight -2)));

    
    //ResultPoints[threadID.x].position = float3(samplePosInUV * float2(2, 2/ aspectRatio) + float2(0,-0.5) - 2 ,0);
    float2 posXY = (samplePosInUV -0.5) * 2 * float2(1/aspectRatio,1);
    ResultPoints[threadID.x].Position = float3(posXY ,0);
    ResultPoints[threadID.x].W = 1;
    ResultPoints[threadID.x].Rotation = float4(0,0,0,1);
    ResultPoints[threadID.x].Color = Image.SampleLevel(texSampler,  samplePosInUV * float2(1, -1) + float2(0,1), 0);
    ResultPoints[threadID.x].Selected = 1;
}
