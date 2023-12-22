cbuffer ParamConstants : register(b0)
{
    float4 Black;
    float4 White;
    float4 GrayScaleWeights;
    float Bias;
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

    //return float4(1,0,1,1);
    // //float2 uv = fragCoord / iResolution.xy;
    float4 color = Image.Sample(texSampler, input.texCoord); 
    //float grayScale = length( color * GrayScaleWeights);
    float4 t = color * GrayScaleWeights;
    float grayScale = (t.r + t.g + t.b + t.a) / 
    (GrayScaleWeights.r + GrayScaleWeights.g + GrayScaleWeights.b + GrayScaleWeights.a);
    
    grayScale = pow(grayScale, Bias);

    float2 fragCoord = input.texCoord * float2(width,height);
    float dithering = (Bayer64(fragCoord * 0.25) * 2.0 - 1.0) * 0.5;

    float blackOrWhite = dithering + grayScale < 0.5 ? 0 : 1;



    return lerp(Black,White, blackOrWhite);
    return color + float4(1,0,0,0);



    // uv.x += dithering;   
   
    // fragColor = float4(uv.x < 0.5);

    
    // float sx = SampleRadius / width;
    // float sy = SampleRadius / height;
    
    // float x = input.texCoord.x;
    // float y = input.texCoord.y;

    // float4 y1= Image.Sample(texSampler, float2(input.texCoord.x,       input.texCoord.y + sy));
    // float4 y2= Image.Sample(texSampler, float2(input.texCoord.x,       input.texCoord.y - sy));
    
    // float4 x1= Image.Sample(texSampler,  float2(input.texCoord.x + sx, input.texCoord.y));
    // float4 x2= Image.Sample(texSampler,  float2(input.texCoord.x - sx, input.texCoord.y)); 
    // float4 m =  Image.Sample(texSampler, float2(input.texCoord.x,      input.texCoord.y)); 
    // //return ((m-y1) + (m-y2) + (m-x1) + (m-x2)) * Strength;
    
    // float average =  (           
    //                 abs(x1.r-m.r) + abs(x2.r-m.r) + abs(y1.r - m.r) +abs(y2.r - m.r) +
    //                 abs(x1.g-m.g) + abs(x2.g-m.g) + abs(y1.g - m.g) +abs(y2.g - m.g) +
    //                 abs(x1.b-m.b) + abs(x2.b-m.b) + abs(y1.b - m.b) +abs(y2.b - m.b)
    //             ) * Strength + Contrast;
                

    // float4 edgeColor = OutputAsTransparent < 0.5 
    //         ? clamp(float4(average,average,average,1),0 , 10000) * Color
    //         : float4(Color.rgb, clamp(average,0, 1));
    // return lerp(edgeColor, m, MixOriginal);
}