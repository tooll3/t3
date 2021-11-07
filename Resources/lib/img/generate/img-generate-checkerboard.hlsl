cbuffer ParamConstants : register(b0)
{
    float4 ColorA;
    float4 ColorB;
    float2 Size;
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

// Texture2D<float4> ImageA : register(t0);
// sampler texSampler : register(s0);

#define mod(x, y) (x - y * floor(x / y))
float IsBetween( float value, float low, float high) {
    return (value >= low && value <= high) ? 1:0;
}


float4 psMain(vsOutput psInput) : SV_TARGET
{    
    float2 p = psInput.texCoord / Size;
    float2 a = mod(p,1);
    float t= (a.x > 0.5 && a.y < 0.5) ||  (a.x < 0.5 && a.y > 0.5) ? 0 :1;
    return lerp(ColorA, ColorB,  t);
}