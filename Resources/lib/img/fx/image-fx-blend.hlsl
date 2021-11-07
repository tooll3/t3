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
sampler texSampler : register(s0);


float IsBetween( float value, float low, float high) {
    return (value >= low && value <= high) ? 1:0;
}


float4 psMain(vsOutput psInput) : SV_TARGET
{    
    float4 tA = ImageA.Sample(texSampler, psInput.texCoord) * ImageAColor; 
    float4 tB = ImageB.Sample(texSampler, psInput.texCoord) * ImageBColor;    
    tA.a = clamp(tA.a, 0,1);

    tB.a = clamp(tB.a, 0,1);

    float a = tA.a + tB.a - tA.a*tB.a;    
    float3 rgb = (1.0 - tB.a)*tA.rgb + tB.a*tB.rgb;   
    
    switch( (int)ColorMode) {
        // screen
        case 1:
            rgb = 1-(1-tA.rgb) * (1-tB.rgb);
            break;
            
        // multiply
        case 2:
            rgb = tA.rgb * tB.rgb;
            break;
        //
        case 3:
            rgb = tA.rgb + tB.rgb;
            break;
        
        // overlay
        case 4:
            rgb = float3( 
                tA.r < 0.5?(2.0 * tA.r * tB.r) : (1.0-2.0*(1.0-tA.r)*(1.0- tB.r)),
                tA.g < 0.5?(2.0 * tA.g * tB.g) : (1.0-2.0*(1.0-tA.g)*(1.0- tB.g)),
                tA.b < 0.5?(2.0 * tA.b * tB.b) : (1.0-2.0*(1.0-tA.b)*(1.0- tB.b)));
            break;
            
        // difference
        case 5:
            rgb = abs(tA.rgb - tB.rgb) * tB.a + tB.rgb * (1.0 - tB.a);
            break;        
        case 6:
            rgb = tA.rgb;
            break;
    }
    
    switch( (int)AlphaMode) {
        case 1:
            a = tA.a;
            break;
        case 2:
            a = tA.a * tB.a;
            break;
            
        case  3:
            a =1;
            break;
    }
    
    return float4(rgb, a);
}