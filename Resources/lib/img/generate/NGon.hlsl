cbuffer ParamConstants : register(b0)
{
    float4 Fill;
    float4 Background;
    float2 Position;
    float Round;
    float Feather;
    float GradientBias;
    float Rotate;
    float Sides;
    float Radius;
    float Curvature;
    float Blades;
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



// float sdBox( in float2 p, in float2 b )
// {
//     float2 d = abs(p)-b;
//     return length(
//         max(d,float2(0,0))) + min(max(d.x,d.y), 
//         0.0);
// }

//#define PI    3.14159265358979323846
//#define TAU (PI*2.0)
// float PI = 3.14159265358979323846;
// float TAU = 3.14159265358979323846*2;

static float PI = 3.141592653;
static float TAU = (3.1415926535 * 2);

float mod(float x, float y) {
    return (x - y * floor(x / y));
} 

// based on https://www.shadertoy.com/view/Wll3R2
float sdNgon(in float2 p, in float r, in float n) {
    // can precompute these
	float inv_n = 1.0 / n;
    
    // perform radial repeat
	float2 rp = float2(atan2(p.y, p.x), length(p)); // into polar coords

	//rp.x *= (1.0 / TAU);
    rp.x /= TAU;
    
	rp.x = mod(rp.x + inv_n * 0.5, inv_n) - 0.5 * inv_n;

    rp.x *=  rp.x > 0 ? (1-saturate(Blades)) : 1;
    //rp.x *=  Curvature;
    //float centricity= abs(rp.x);
    rp.y = saturate(lerp(rp.y, r, Curvature));
	rp.x *= TAU;

	p = float2(cos(rp.x), sin(rp.x))*rp.y; // back to cartesian
    
// #ifdef CIRCUMSCRIBE
//     float s = cos(TAU * inv_n * 0.5); // scale by 1.0 / vertex_radius
//     p /= s;
// #endif
    // distance to a "box side"
    float2 b = float2(r,r);
    b.y = b.x * tan(TAU * inv_n * 0.5);
    float2 d = abs(p)-b;
    
    float sd = length(max(d,float2(0,0))) + min(d.x,0.0);
    
// #ifdef CIRCUMSCRIBE
//     return sd * s;
// #else
    return sd;
// #endif
}


float4 psMain(vsOutput psInput) : SV_TARGET
{
    float aspectRatio = TargetWidth/TargetHeight;


    float2 p = psInput.texCoord;
    //p.x -= 0.5;
    p -= 0.5;
    p.x *= aspectRatio;
    
    // // Rotate
    float imageRotationRad = (-Rotate - 90) / 180 *3.141578;     

    float sina = sin(-imageRotationRad - 3.141578/2);
    float cosa = cos(-imageRotationRad - 3.141578/2);

    //p.x *=aspectRatio;

    p = float2(
        cosa * p.x - sina * p.y,
        cosa * p.y + sina * p.x 
    );

    p-=Position * float2(1,-1);
    
    float d = sdNgon(p, Radius, Sides);
    //return float4(d,0,0,1);
    d = smoothstep(Round/2 - Feather/4, Round/2 + Feather/4, d);

    float dBiased = GradientBias>= 0 
        ? pow( d, GradientBias+1)
        : 1-pow( clamp(1-d,0,10), -GradientBias+1);

    float4 c= lerp(Fill, Background,  dBiased);

    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);
    //orgColor = float4(1,1,1,0);
    float a = clamp(orgColor.a + c.a - orgColor.a*c.a, 0,1);

    // FIXME: blend
    //float mixA = a;
    //float3 rgb = lerp(orgColor.rgb, c.rgb,  mixA);    
    float3 rgb = (1.0 - c.a)*orgColor.rgb + c.a*c.rgb;   
    return float4(rgb,a);
}