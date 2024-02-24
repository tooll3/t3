//#include "lib/shared/bias-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float NumSamples;
    float Density;
    float Weight;
    float Amount;
    float Decay;

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
Texture2D<float4> FxImage : register(t1);
sampler texSampler : register(s0);

float4 psMain(vsOutput input) : SV_TARGET
{
    
    float2 centerproof = Center * float2 (1, -1) +float2(0.5,0.5);
    //float width, height;
    //Image.GetDimensions(width, height);
    //float aspectRatio = width / height;
       
    float2 uv = input.texCoord;

    //delta between current pixel and light position
    float2 delta = uv - centerproof;
    
    //define sampling step
    delta *= 1.0f / float(NumSamples) * Density;
    
    //initial color
    float4 color = Image.Sample(texSampler, uv);
    float4 fx = FxImage.Sample(texSampler, uv);
    
    //float illuminationDecay = 0.9f;
    float illuminationDecay = Decay;
    float weight = Weight*0.01; // Scaling down the value in order to make it easier to tweak in the UI
    
    for(int i = 0; i < NumSamples; i++)
    {
        //peform sampling step
        uv -= delta;
        
        //decay the ray
        float4 color_sample = Image.Sample(texSampler, uv) * FxImage.Sample(texSampler, uv);
        
        color_sample *= illuminationDecay * weight;
                
        //original color + ray sample
        color += color_sample * Amount;
        //color = 1 - (1 - color) * (1 - color_sample * Amount ); //screen blending mode
       
    }

    // Output to screen
       
    float4 c = color;
    
    return (c);
}