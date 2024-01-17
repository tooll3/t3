#include "lib/shared/bias-functions.hlsl"

// This shader is based on a ShaderToy Project by jamelouis https://www.shadertoy.com/view/3dXyRl 
// Ported to Tooll3 by Newemka (so you know who to blame)

cbuffer ParamConstants : register(b0)
{
    float4 ColorA;
    float4 ColorB;

    float2 Offset;
    float2 Stretch;

    float Scale;
    float Phase;

    float2 Clamping;
    float2 BiasAndGain;

    float Method;
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

Texture2D<float4> inputTexture : register(t0);
sampler texSampler : register(s0);


// from https://www.shadertoy.com/view/3dXyRl

#define UIF (1.0 / float(0xffffffffU))



/* float2 hash22(float2 p)//Dave Hoskins https://www.shadertoy.com/view/4djSRW
{
    //return frac(cos(mul(p,float2x2(-64.2,71.3,81.4,-29.8)) * 8321.3)); 
    return frac(cos(mul(p,float2x2(-64.2,71.3,81.4,-29.8)) * (Phase + 8321.3)));
} */

// The original hash was not looking good enough 

// This one is gorgeous!! Hash22 from Hash without Sine 2 https://www.shadertoy.com/view/XdGfRR 
float2 hash22(float2 p)
{
	uint2 q = uint2(int2(p))*uint2(1597334673U, 3812015801U);
	q = (q.x ^ q.y) * uint2(1597334673U, 3812015801U);
	return float2(q) * UIF + Phase; // It's amazing + Phase is doing a perfect job
}



float Worley(float2 q, float scale)
{
    int wt = 0;
    int f2t = 0;

    //worley F1
    if (Method < 1){
        wt = 0;
        f2t = 0;
    }
    //manhattan worley F1
    else if (Method < 2){
        wt = 1;
        f2t = 0;
    }
    //chebyshev worley F1 
    else if (Method < 3){
        wt = 2;
        f2t = 0;
    }
    //worley F2-F1
    else if (Method < 4){
        wt = 0;
        f2t = 1;
    }
    //manhattan worley F2-F1
    else if (Method < 5){
        wt = 1;
        f2t = 1;
    }
    //chebyshev worley F2-F1
    else if (Method < 6){
        wt = 2;
        f2t = 1;
    }
    
    q = q/scale;
    float f1 = 9e9;
    float f2 = f1;
    for(int i = -1; i < 2; i++){
        for(int j = -1; j < 2; j++){
            float2 p = floor(q) + float2(i, j);
            float2 h = hash22(p);
            float2 g = p + 0.5+ 0.5 * sin(h*12.6);
            float d = f1;
            if(wt == 0) {
                d = distance(g,q);
            }else if(wt == 2) {
            	float xx = abs(q.x-g.x);
            	float yy = abs(q.y-g.y);
            	d = max(xx, yy);
            } else{
                float xx = abs(q.x-g.x);
            	float yy = abs(q.y-g.y);
                d = xx + yy;
            }
            if(d < f2){ f2 = d; }
            if(d < f1){f2 = f1; f1 = d; }
        }
    }
    if(f2t == 0){
        return f1;
    }
    	
    else{
        return f2 - f1;
    }
        
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    //scaling variables for better UX in T3 UI
    float scale = Scale * 0.001;
    float2 offset = Offset * float2(-100,100);

    float aspectRatio = TargetWidth / TargetHeight;
    float2 uv = psInput.texCoord;
    uv -= 0.5;
    uv /= Stretch * scale;
    uv += offset;
    uv.x *= aspectRatio;

    float worley = GetBiasGain( Worley(uv, 32.0), BiasAndGain.x, BiasAndGain.y);
    return lerp(ColorB, ColorA, clamp(worley, Clamping.x, Clamping.y ) );
}