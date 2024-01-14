#include "lib/shared/blend-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float4 Color;
    float Range;
    float Brightness;
    float Threshold; // Color key threshold (0-1)
    float BlendMode;

    float4 OriginalColor;
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


float4 psMain(vsOutput input) : SV_TARGET
{
    float2 uv = input.texCoord;
    
    float width, height;
    Image.GetDimensions(width, height);

    const float range = 0.3; // Length of glow streaks
    const float steps = 0.002; // Number of texture samples / 2

    float4 streaksColor = float4(0.0,0.0,0.0,1.0);
    // (Composite > 0.5) ? Image.Sample(texSampler, uv )
    //                                       : float4(0.0,0.0,0.0,1.0);
   
    
    for (float i = -Range; i < Range; i += steps) {
    
        float falloff = 1.0 - abs(i / Range);
    
        float4 blur = Image.Sample(texSampler, uv + i);
        if (blur.r + blur.g + blur.b > Threshold * 3.0) {
            streaksColor += blur * falloff * steps * Brightness;
        }
        
        blur = Image.Sample(texSampler, uv + float2(i, -i));
        if (blur.r + blur.g + blur.b > Threshold * 3.0) {
            streaksColor += blur * falloff * steps * Brightness;
        }
    }
    

    //float4 c= lerp(Fill, Background,  dBiased);
    float4 orgColor = Image.Sample(texSampler, uv) * OriginalColor;
    //float a = clamp(orgColor.a + streaksColor.a - orgColor.a*streaksColor.a, 0,1);

    return  BlendColors(orgColor, streaksColor * Color, (int)BlendMode);

    //return (streaksColor);
}