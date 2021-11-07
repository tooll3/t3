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
    //float4 orgColor = inputTexture.SampleLevel(texSampler, uv, 0.0);

    float aspectRation = TargetWidth/TargetHeight;
    float2 p = uv;
    p-= 0.5;
    p.x *=aspectRation;

    //float2 p = psInput.texCoord;

    // float radians = Rotation / 180 *3.141578;
    // float2 angle =  float2(sin(radians),cos(radians));

    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);

    // float c=  dot(p-Center, angle);
    float c = distance(p, Center) * 2;

    c = PingPong > 0.5 
        ? (Repeat < 0.5 ? (abs(c) / Width)
                        : 1-abs( fmod(c,Width *2) -Width)  / Width)
        : c / Width;

    c = Repeat > 0.5 
        ? fmod(c,1)
        : saturate(c);

    // if(Smooth > 0.5) {
    //     c= smoothstep(0,1,c);
    // }

    float dBiased = Bias>= 0
        ? pow( c, Bias+1)
        : 1-pow( clamp(1-c,0,10), -Bias+1);
    
    dBiased= clamp(dBiased,0.001, 0.999);
    //return float4(dBiased,0,0,1);
    //float4 cOut= lerp(Fill, Background, dBiased);


    float4 gradient = Gradient.Sample(texSampler, float2(dBiased, 0));
    float a = orgColor.a + gradient.a - orgColor.a*gradient.a;
    float3 rgb = (1.0 - gradient.a)*orgColor.rgb + gradient.a*gradient.rgb;   

    return float4(rgb,a);
}