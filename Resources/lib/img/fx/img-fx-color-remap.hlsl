cbuffer ParamConstants : register(b0)
{
    float4 Fill;
    float4 Background;
    float2 Center;
    float Width;
    float Rotation;
    float PingPong;
    float Repeat;
    float Bias;
}

cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

cbuffer Resolution : register(b2)
{
    float TargetWidth;
    float TargetHeight;
}


struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> ImageA : register(t0);
Texture2D<float4> Gradient : register(t1);
sampler texSampler : register(s0);

float fmod(float x, float y) {
    return (x - y * floor(x / y));
} 

float4 psMain(vsOutput psInput) : SV_TARGET
{    
    float2 uv = psInput.texCoord;
    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);
    float4 gradient = Gradient.Sample(texSampler, float2(orgColor.r, 0));
    float a = orgColor.a + gradient.a - orgColor.a*gradient.a;
    float3 rgb = (1.0 - gradient.a)*orgColor.rgb + gradient.a*gradient.rgb;   
    return float4(rgb,a);
}