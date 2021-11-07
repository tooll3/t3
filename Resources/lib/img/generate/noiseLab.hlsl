// This shader is heavily based on a ShaderToy Project by CandyCat https://www.shadertoy.com/view/4sc3z2

#define Use_Simplex

cbuffer ParamConstants : register(b0)
{
    float4 ColorA;
    float4 ColorB;

    float2 Offset;
    float2 Stretch;

    float Scale;
    float Evolution;
    float Bias;
    float Iterations;

    float3 WarpOffset;
    float TestParam;
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


float IsBetween( float value, float low, float high) {
    return (value >= low && value <= high) ? 1:0;
}



// ========= Hash ===========

float3 hashOld33(float3 p)
{   
	p = float3( dot(p,float3(127.1,311.7, 74.7)),
			  dot(p,float3(269.5,183.3,246.1)),
			  dot(p,float3(113.5,271.9,124.6)));
    
    return -1.0 + 2.0 * frac(sin(p)*43758.5453123);
}

float hashOld31(float3 p)
{
    float h = dot(p,float3(127.1,311.7, 74.7));
    
    return -1.0 + 2.0 * frac(sin(h)*43758.5453123);
}

// Grab from https://www.shadertoy.com/view/4djSRW
#define MOD3 float3(.1031,.11369,.13787)
//#define MOD3 float3(443.8975,397.2973, 491.1871)
float hash31(float3 p3) 
{
	p3  = frac(p3 * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return -1.0 + 2.0 * frac((p3.x + p3.y) * p3.z);
}

float3 hash33(float3 p3)
{
	p3 = frac(p3 * MOD3);
    p3 += dot(p3, p3.yxz+19.19);
    return -1.0 + 2.0 * frac(float3((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y, (p3.y+p3.z)*p3.x));
}

// ========= Noise ===========
/*
float value_noise(float3 p)
{
    float3 pi = floor(p);
    float3 pf = p - pi;
    
    float3 w = pf * pf * (3.0 - 2.0 * pf);
    
    return 	lerp(
        		lerp(
        			lerp(hash31(pi + float3(0, 0, 0)), hash31(pi + float3(1, 0, 0)), w.x),
        			lerp(hash31(pi + float3(0, 0, 1)), hash31(pi + float3(1, 0, 1)), w.x), 
                    w.z),
        		lerp(
                    lerp(hash31(pi + float3(0, 1, 0)), hash31(pi + float3(1, 1, 0)), w.x),
        			lerp(hash31(pi + float3(0, 1, 1)), hash31(pi + float3(1, 1, 1)), w.x), 
                    w.z),
        		w.y);
}

float perlin_noise(float3 p)
{
    float3 pi = floor(p);
    float3 pf = p - pi;
    
    float3 w = pf * pf * (3.0 - 2.0 * pf);
    
    return 	lerp(
        		lerp(
                	lerp(dot(pf - float3(0, 0, 0), hash33(pi + float3(0, 0, 0))), 
                        dot(pf - float3(1, 0, 0), hash33(pi + float3(1, 0, 0))),
                       	w.x),
                	lerp(dot(pf - float3(0, 0, 1), hash33(pi + float3(0, 0, 1))), 
                        dot(pf - float3(1, 0, 1), hash33(pi + float3(1, 0, 1))),
                       	w.x),
                	w.z),
        		lerp(
                    lerp(dot(pf - float3(0, 1, 0), hash33(pi + float3(0, 1, 0))), 
                        dot(pf - float3(1, 1, 0), hash33(pi + float3(1, 1, 0))),
                       	w.x),
                   	lerp(dot(pf - float3(0, 1, 1), hash33(pi + float3(0, 1, 1))), 
                        dot(pf - float3(1, 1, 1), hash33(pi + float3(1, 1, 1))),
                       	w.x),
                	w.z),
    			w.y);
}
*/
float simplex_noise(float3 p)
{
    const float K1 = 0.333333333;
    const float K2 = 0.166666667;
    
    float3 i = floor(p + (p.x + p.y + p.z) * K1);
    float3 d0 = p - (i - (i.x + i.y + i.z) * K2);
    
    // thx nikita: https://www.shadertoy.com/view/XsX3zB
    float3 e = step(float3(0,0,0), d0 - d0.yzx);
	float3 i1 = e * (1.0 - e.zxy,1.0 - e.zxy,1.0 - e.zxy);
	float3 i2 = 1.0 - e.zxy * (1.0 - e);
    
    float3 d1 = d0 - (i1 - 1.0 * K2);
    float3 d2 = d0 - (i2 - 2.0 * K2);
    float3 d3 = d0 - (1.0 - 3.0 * K2);
    
    float4 h = max(0.6 - float4(dot(d0, d0), dot(d1, d1), dot(d2, d2), dot(d3, d3)), 0.0);
    float4 n = h * h * h * h * float4(dot(d0, hash33(i)), dot(d1, hash33(i + i1)), dot(d2, hash33(i + i2)), dot(d3, hash33(i + 1.0)));
    
    return dot(float4(31.316, 31.316, 31.316, 31.316), n);
}

float noise(float3 p) {
#ifdef Use_Perlin
    return perlin_noise(p * 2.0);
#elif defined Use_Value
    return value_noise(p * 2.0);
#elif defined Use_Simplex
    return simplex_noise(p*1);
#endif
    
    return 0.0;
}

// ========== Different function ==========

float noise_itself(float3 p)
{
    return noise(p * 8.0);
}

float noise_sum(float3 p)
{
    float f = 0.0;
    p = p * 4.0;
    f += 1.0000 * noise(p); p = 2.0 * p;
    f += 0.5000 * noise(p); p = 2.0 * p;
	f += 0.2500 * noise(p); p = 2.0 * p;
	f += 0.1250 * noise(p); p = 2.0 * p;
	f += 0.0625 * noise(p); p = 2.0 * p;
    
    return f/2+1;
}

float noise_sum_abs(float3 p)
{
    float f = 0.0;
    p = p * 1.0;
    f += 1.0000 * abs(noise(p)); p = 2.0 * p;
    f += 0.5000 * abs(noise(p)); p = 2.0 * p;
	f += 0.2500 * abs(noise(p)); p = 2.0 * p;
	f += 0.1250 * abs(noise(p)); p = 2.0 * p;
	f += 0.0625 * abs(noise(p)); p = 2.0 * p;
    
    return f;
}


float noise_sum_abs_sin(float3 p)
{
    float f = noise_sum_abs(p);    
    return f ;
}


float4 psMain(vsOutput psInput) : SV_TARGET
{   
    float aspectRatio = TargetWidth/TargetHeight; 
	float2 uv = psInput.texCoord;     
    uv-= 0.5;
    uv/= Stretch * Scale; 
    uv+= Offset * float2(-1 / aspectRatio,1);
    uv.x*= aspectRatio;
    float3 pos = float3(uv, Evolution/10);

    int steps = clamp( Iterations + 0.5, 1.1,5.1);

    float f = 0.7;
    float scaleFactor = 1;
    for(int i = 0; i < steps ; i++) 
    {
        float f1 = noise_sum_abs(pos * scaleFactor + float3(12.4,3,0) * i);
        scaleFactor *= TestParam;
        pos += f * WarpOffset;
        f *= sin(f1 )/2 + 0.5;
        f+=0.2;
    }
    f *= 2;
    f -=1;

    // float f2 = noise_sum_abs(pos / 0.2 + float3(12.4,3,0));    
    // pos += f2 * WarpOffset;

    // float f3 = noise_sum_abs(pos / 5 + float3(2,3,0));    
    // f *= sin(f3) / 2 + 0.5;
    //f = (f * f2 *f3)* 10;

    float fBiased = Bias>= 0 
        ? pow( f, Bias+1)
        : 1-pow( clamp(1-f,0,10), -Bias+1);    

    return lerp(ColorA, ColorB, fBiased);
}