cbuffer ParamConstants : register(b0)
{
    float4 ImageAColor;
    float4 ImageBColor;
    float ColorMode;
    float AlphaMode;
}

cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> ImageA : register(t0);
Texture2D<float4> ImageB : register(t1);
Texture2D<float4> Mask : register(t2);
sampler texSampler : register(s0);


float IsBetween( float value, float low, float high) {
    return (value >= low && value <= high) ? 1:0;
}


float4 psMain(vsOutput psInput) : SV_TARGET
{    
    float4 tA = ImageA.Sample(texSampler, psInput.texCoord) * ImageAColor; 
    float4 tB = ImageB.Sample(texSampler, psInput.texCoord) * ImageBColor;    
    float4 mask = Mask.Sample(texSampler, psInput.texCoord);    

    tA.a = clamp(tA.a, 0,1);
    tB.a = clamp(tB.a, 0,1);

    return lerp(tA, tB, mask.r);
}