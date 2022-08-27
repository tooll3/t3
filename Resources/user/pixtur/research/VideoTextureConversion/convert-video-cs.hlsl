#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer ParamConstants : register(b0)
{
    float4 Color;
}


Texture2D<float4> FxTexture : register(t0);
//sampler texSampler : register(s0);

RWTexture2D<float4> WriteOutput  : register(u0); 

[numthreads(32,32,1)]
void main(uint3 i : SV_DispatchThreadID)
{   
    int texWidth;
    int texHeight;
    WriteOutput.GetDimensions(texWidth, texHeight);
    if(i.x >= texWidth || i.y >= texHeight){
        return;
    }

    float2 res= float2(texWidth, texHeight);
    //float2 uv = i.xy / res;
    //float4 inputColor = FxTexture.SampleLevel(texSampler, uv, 0);

    float4 inputColor = FxTexture[i.xy];

    WriteOutput[i.xy] = inputColor * Color + 0.2; 
}
