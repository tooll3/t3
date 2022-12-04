#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"


cbuffer Params : register(b0)
{
    float TriggerClear;
}

Texture2D<float4> SourceImage : register(t0);
RWTexture2D<float4> ResultImage : register(u0);

[numthreads(1,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    int sourceWidth,sourceHeight, numLevels;
    SourceImage.GetDimensions(0, sourceWidth, sourceHeight, numLevels);

    int resultWidth,resultHeight;
    ResultImage.GetDimensions( resultWidth, resultHeight);

    int x = i.x;
    if(i.y > 0)
        return;

    int yButtom = resultHeight - 1;

    // scrolling up
    // for(int y = 0; y < resultHeight; y++ )
    // {
    //     ResultImage[ int2(x, y)] = ResultImage[ int2(x, y+1)];
    // }
    // ResultImage[ int2(x, yButtom)] = SourceImage[int2(x,0)];

    // Scrolling down
    for(int y = resultHeight -1; y > 0; y-- )
    {
        ResultImage[ int2(x, y)] = ResultImage[ int2(x, y-1)];
    }
    ResultImage[ int2(x, 0)] = SourceImage[int2(x,0)];
}

