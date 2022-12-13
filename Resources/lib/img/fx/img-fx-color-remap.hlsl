cbuffer ParamConstants : register(b0)
{
    float Bias;
    float DontColorAlpha;
    float Mode;
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

    float4 gradient = 0;
    if(Mode < 0.5) {
        float gray= (orgColor.r + orgColor.g + orgColor.b) / 3;
        gradient = Gradient.Sample(texSampler, float2(gray, 0));
    } 
    else {

        gradient = float4( 
            Gradient.Sample(texSampler, float2(orgColor.r, 0)).r,
            Gradient.Sample(texSampler, float2(orgColor.g, 0)).g,
            Gradient.Sample(texSampler, float2(orgColor.b, 0)).b,
            Gradient.Sample(texSampler, float2(orgColor.a, 0)).a);
    }

    float a =  DontColorAlpha < 0.5 
                    ? gradient.a
                    : (orgColor.a + gradient.a - orgColor.a * gradient.a);

    float3 rgb = (1.0 - gradient.a)*orgColor.rgb + gradient.a*gradient.rgb;   
    return float4(rgb,a);
}