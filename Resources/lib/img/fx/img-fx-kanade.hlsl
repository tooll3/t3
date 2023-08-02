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

// float4 psMain(vsOutput psInput) : SV_TARGET{
//     float width, height;
//     inputTextureA.GetDimensions(width, height);
//     float2 iResolution = float2(width, height).xy;
//     // return float4(1,1,1,1);
//     // float aspect = width / height;

//     // float2 uv = psInput.texCoord;
//     // float2 p = uv;
//     // p.x *= aspect;
//     float blockSize = 5. + iResolution.y / 50.;
//     float2 within_block = float2(mod(psInput.texCoord.x, blockSize) - .5 * blockSize,mod(psInput.texCoord.y, blockSize) - .5*blockSize);
//     float2 block = psInput.position.xy - within_block;
//     float2 uv = block.xy / iResolution.xy;
//     float2 flow = kanade(block.xy, Lod).rg - float2(.5, .5);
    
//     float lineness = abs(dot(normalize(flow.yx * float2(-1.,1.)), within_block));
//     float alongness = (dot(flow, within_block) / blockSize);
//     float dark = smoothstep(.2 * blockSize, .0, lineness) *
//         step(alongness, dot(flow, flow)) * step(.0, alongness);
//     float ballness = smoothstep(3., 1., dot(within_block, within_block));
//     if (dot(flow, flow) < 1.e-6) {
//         return float4(ballness.rrr,  1.);
//     } else {
//         return float4((dark + ballness).rrr, 1.);
//     }
// }
