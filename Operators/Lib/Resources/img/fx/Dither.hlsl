cbuffer ParamConstants : register(b0)
{
    float4 Black;
    float4 White;
    float4 GrayScaleWeights;
    float Bias;
    float Scale;
}

cbuffer Resolution : register(b1)
{
    float TargetWidth;
    float TargetHeight;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> Image : register(t0);
sampler texSampler : register(s0);

#define Bayer4(a)   (Bayer2 (.5 *(a)) * .25 + Bayer2(a))
#define Bayer8(a)   (Bayer4 (.5 *(a)) * .25 + Bayer2(a))
#define Bayer16(a)  (Bayer8 (.5 *(a)) * .25 + Bayer2(a))
#define Bayer32(a)  (Bayer16(.5 *(a)) * .25 + Bayer2(a))
#define Bayer64(a)  (Bayer32(.5 *(a)) * .25 + Bayer2(a))

inline float Bayer2(float2 a) {
    a = floor(a);
    return frac(a.x / 2. + a.y * a.y * .75);
}

float4 psMain(vsOutput input) : SV_TARGET
{
    float width, height;
    Image.GetDimensions(width, height);
    float2 res = float2(width,height);

    float4 color = Image.Sample(texSampler, (int2)(input.texCoord * res / Scale) / res * Scale ); 
    float4 t = color * GrayScaleWeights;
    float grayScale = (t.r + t.g + t.b + t.a) / 
    (GrayScaleWeights.r + GrayScaleWeights.g + GrayScaleWeights.b + GrayScaleWeights.a);
    
    grayScale = pow(grayScale, Bias);

    float2 fragCoord = input.texCoord * res;
    float dithering = (Bayer64(fragCoord / Scale) * 2.0 - 1.0) * 0.5;

    float blackOrWhite = dithering + grayScale < 0.5 ? 0 : 1;
    return lerp(Black,White, blackOrWhite);
}  