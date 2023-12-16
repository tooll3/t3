// This shader is based on Shard Noise by ENDESGA: https://www.shadertoy.com/view/dlKyWw
// Ported to tooll3 by Newemka


cbuffer ParamConstants : register(b0)
{
    float4 ColorA;
    float4 ColorB;
    float2 Position;
    float2 Stretch;
    float Scale;
    float Fade;
    float GradientBias;
    float Phase;
    float BlendMode;
    float IsTextureValid;
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

Texture2D<float4> ImageA : register(t0);
sampler texSampler : register(s0);

float3 hash(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5,183.3,246.1)), dot(p, float3(113.5, 271.9, 124.6)));
    p = frac(sin(p) * 43758.5453123);
    return p;
}
#define tau 6.283185307179586

float shard_noise(in float3 p, in float sharpness) {
    float3 ip = floor(p);
    float3 fp = frac(p);

    float v = 0., t = 0.;
    for (int z = -1; z <= 1; z++) {
        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                float3 o = float3(x, y, z);
                float3 io = ip + o;
                float3 h = hash(io);
                float3 r = fp - (o + h);

                float w = exp2(-tau*dot(r, r));
                // tanh deconstruction and optimization by @Xor
                float s = sharpness * dot(r, hash(io + float3(11, 31, 47)) - 0.5);
                v += w * s*rsqrt(1.0+s*s);
                t += w;
            }
        }
    }
    return ((v / t) * .5) + .5;
}



float4 psMain(vsOutput psInput) : SV_TARGET
{
    float aspectRatio = TargetWidth/TargetHeight;

    float2 p = psInput.texCoord;
    //p.x -= 0.5;
    p -= 0.5;
    p.x *= aspectRatio;
    p /= Stretch;

  //float2 p = F/R.y;
    float3 uv = float3( p + Position, Phase * .1 );
   
    float fade = Fade*128; //Fade *128;
    float fade2 = 4;
    //float fade =  pow(p.x,2.) * 30.;
    float4 c = float4(0,0,0,1);

    //Shard Noise
    float3 sn = float(shard_noise(Scale * uv, fade)*GradientBias);

    //Octave-blend
    float3 o = float(
            (shard_noise(64.0*uv,fade2) * .03125) +
            (shard_noise(32.0*uv,fade2) * .0625) +
            (shard_noise(16.0*uv,fade2) * .125) +
            (shard_noise(8.0*uv,fade2) * .25) +
            (shard_noise(4.0*uv,fade2) * .5)
        ); // octave-blend 
    /* if(p.y<0.5 ) 
    {
        c = float4(float3(
            (shard_noise(64.0*uv,fade) * .03125) +
            (shard_noise(32.0*uv,fade) * .0625) +
            (shard_noise(16.0*uv,fade) * .125) +
            (shard_noise(8.0*uv,fade) * .25) +
            (shard_noise(4.0*uv,fade) * .5)
        ),1.);
    }
    else{
    c = float4( float3(shard_noise(16.0*uv,fade)), 1. );}
    if((p.y > .875 || p.y < .125)) c = round(c); */
    //c = float4(sn,1);
    c = float4(saturate(sn),1);
    
    return lerp(ColorA,ColorB,c) ;
}