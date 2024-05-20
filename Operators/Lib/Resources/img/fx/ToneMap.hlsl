//RWTexture2D<float4> outputTexture : register(u0);
Texture2D<float4> inputTexture : register(t0);
sampler texSampler : register(s0);

cbuffer ParamConstants : register(b0)
{
    float Mode;
    float CorrectGamma;
    float GammaValue;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};


float3 uncharted2Tonemap(const float3 x) {
	const float A = 0.15;
	const float B = 0.50;
	const float C = 0.10;
	const float D = 0.20;
	const float E = 0.02;
	const float F = 0.30;
	return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

float3 tonemapUncharted2(const float3 color) {
	const float W = 11.2;
	const float exposureBias = 2.0;
	float3 curr = uncharted2Tonemap(exposureBias * color);
	float3 whiteScale = 1.0 / uncharted2Tonemap(float3(W.xxx));
	return curr * whiteScale;
}

// Based on Filmic Tonemapping Operators http://filmicgames.com/archives/75
float3 tonemapFilmic(const float3 color) {
	float3 x = max(float3(0,0,0), color - 0.004);
	return (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06);
}

// https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
float3 acesFilm(const float3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d ) + e), 0.0, 1.0);
}


float3 tonemapReinhard(const float3 color) {
	return color / (color + 1);
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float2 uv = psInput.texCoord;
    float4 c = inputTexture.SampleLevel(texSampler, uv, 0.0);
    
    if(Mode < 0.5) {
        c= float4( acesFilm(c.rgb), 1);
    }
    else if (Mode < 1.5) {
        c= float4( tonemapReinhard(c.rgb), 1);
    }
    else if (Mode < 2.5) {
        c= float4( tonemapFilmic(c.rgb), 1);
    }
    else if (Mode < 3.5) {
        c= float4( tonemapUncharted2(c.rgb), 1);
    }
    
    if(CorrectGamma > 0.5) {
        float gamma = GammaValue;
        c.rgb = pow(c.rgb, 1.0/gamma);
    }

    return c;
}
