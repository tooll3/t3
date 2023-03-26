cbuffer ParamConstants : register(b0)
{
    float4 Fill;
    float4 Background;
    float2 Size;
    float2 Position;
    float Round;
    float Feather;
    float GradientBias;
    float Rotate;
    float ColorMode;
    float IsTextureValid;
}

// cbuffer TimeConstants : register(b1)
// {
//     float globalTime;
//     float time;
//     float runTime;
//     float beatTime;
// }

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

Texture2D<float4> ImageA : register(t0);
sampler texSampler : register(s0);

float sdBox(in float2 p, in float2 b)
{
    float2 d = abs(p) - b;
    return length(
               max(d, float2(0, 0))) +
           min(max(d.x, d.y),
               0.0);
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float aspectRatio = TargetWidth / TargetHeight;

    float2 p = psInput.texCoord;
    // p.x -= 0.5;
    p -= 0.5;
    p.x *= aspectRatio;

    // Rotate
    float imageRotationRad = (-Rotate - 90) / 180 * 3.141578;

    float sina = sin(-imageRotationRad - 3.141578 / 2);
    float cosa = cos(-imageRotationRad - 3.141578 / 2);

    // p.x *=aspectRatio;

    p = float2(
        cosa * p.x - sina * p.y,
        cosa * p.y + sina * p.x);

    p -= Position * float2(1, -1);

    float d = sdBox(p, Size / 2);

    d = smoothstep(Round / 2 - Feather / 4, Round / 2 + Feather / 4, d);

    float dBiased = GradientBias >= 0
                        ? pow(d, GradientBias + 1)
                        : 1 - pow(clamp(1 - d, 0, 10), -GradientBias + 1);

    float4 c = lerp(Fill, Background, dBiased);

    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);
    // orgColor = float4(1,1,1,0);
    float a = clamp(orgColor.a + c.a - orgColor.a*c.a, 0,1);

    // Mab mess blend modes 
     float3 rgb = 1;

     switch( (int)ColorMode) {
        // normal
        case 0:
            rgb = (1.0 - c.a)*orgColor.rgb + c.a*c.rgb;
            break;
            
        // screen
        case 1:
            rgb = 1-(1-orgColor.rgb) * (1-c.rgb * c.a);            
            break;
    
        // multiply
        case 2:
            rgb =  lerp(orgColor.rgb, orgColor.rgb * c.rgb, c.a);
            break;
        // overlay
        case 3:
            rgb =  float3( 
                orgColor.r < 0.5?(2.0 * orgColor.r * c.r) : (1.0-2.0*(1.0-orgColor.r)*(1.0- c.r)),
                orgColor.g < 0.5?(2.0 * orgColor.g * c.g) : (1.0-2.0*(1.0-orgColor.g)*(1.0- c.g)),
                orgColor.b < 0.5?(2.0 * orgColor.b * c.b) : (1.0-2.0*(1.0-orgColor.b)*(1.0- c.b)));
                
            rgb = lerp(orgColor.rgb, rgb, c.a);
            break;
            
        // difference
        case 4:
            rgb = abs(orgColor.rgb - c.rgb) * c.a + c.rgb * (1.0 - c.a);
            break;        

               
    }
    return float4(rgb,a);
    

    return (IsTextureValid < 0.5) ? c
                                  : float4((1.0 - c.a) * orgColor.rgb + c.a * c.rgb,
                                           orgColor.a + c.a - orgColor.a * c.a);
}