// this code implements this shader in hlsl, https://www.shadertoy.com/view/XlBcRV
sampler texSampler : register(s0);
Texture2D<float4> inputTextureA : register(t0);
Texture2D<float4> inputTextureB : register(t1);

cbuffer ParamConstants : register(b0)
{
    float Lod;
}


cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

cbuffer Resolution : register(b2)
{
    float TargetWidth;
    float TargetHeight;
}


struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};


#define mod(x,y) (x-y*floor(x/y))


float intensity(float2 loc, float time) {
    float i0 = dot(inputTextureA.SampleLevel(texSampler, loc, Lod).rgb, float3(1,1,1));
    float i1 = dot(inputTextureB.SampleLevel(texSampler, loc, Lod).rgb, float3(1,1,1));
    return lerp(i0, i1, time);
}

float4 psMain(vsOutput psInput) : SV_TARGET{
    // lucas-kanade optical flow 
    // https://en.wikipedia.org/wiki/Lucas%E2%80%93Kanade_method
    float LodScale = pow(2., Lod);
    float2x2 AtA = float2x2(0.0, 0.0, 0.0, 0.0);
    float2 Atb = float2(0.0, 0.0);
    float width, height;
    inputTextureA.GetDimensions(width, height);
    float2 iResolution = float2(width, height).xy;

    float2 p = (psInput.position.xy - float2(3., 3.)) / iResolution.xy;
    float xstart = p.x;
    float2 px_step = LodScale / iResolution.xy;

    for (int i = 0; i < 7; ++i) {
        p.x = xstart;
        for (int j = 0; j < 7; ++j) {
            float I = intensity(p, 0.0);
            float It = I - intensity(p, 2.0);
            float Ix = intensity(p + float2(1.0, 0.0) * px_step, 0.0) - I;
            float Iy = intensity(p + float2(0.0, 1.0) * px_step, 0.0) - I;
            
            AtA += float2x2(Ix * Ix, Ix * Iy, Ix * Iy, Iy * Iy);
            Atb -= float2(It * Ix, It * Iy);
            p.x += px_step.x;
        }
        p.y += px_step.y;
    }
    float2x2 AtAinv = float2x2(AtA[0][0], -AtA[0][1], -AtA[1][0], AtA[1][1]) /
        (AtA[0][0] * AtA[1][1] - AtA[1][0] * AtA[0][1]);

    float2 flow = mul(AtAinv, Atb);
    return float4(0.5 + .1 * flow, .0, 1.);
}
