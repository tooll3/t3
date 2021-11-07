Texture2D<float4> ImageTexture : register(t0);
Texture2D<float4> DepthTexture : register(t1);
Texture2D<float4> NoiseTexture : register(t2);
sampler texSampler : register(s0);

cbuffer ParamConstants : register(b0)
{
    //float2 DepthRange;
    float Near;
    float Far;
    float2 DepthClip;

    float4 Color;

    float2 BoostShadows;
    float Passes;
    float KernelSize;

    float MixOriginal;
    float MultiplyOriginal;
    float2 NoiseOffset;
}

// cbuffer TimeConstants : register(b1)
// {
//     float globalTime;
//     float time;
//     float runTime;
//     float beatTime;
// }

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

// float SchlickBias(float x, float bias)
// {
//     float x1 = x * 2;
//     float x2 = x * 2 - 1;
//     float bias1 = 1 - bias;
//     return x < 0.5
//         ? x1 / ((1 / bias - 2) * (1 - x1) + 1) / 2
//         : x2 / ((1 / bias1 - 2) * (1 - x2) + 1) / 2 + 0.5;
// }

float DepthToLinear(float depth) 
{
    return (2.0 * Near) / (Far + Near - depth * (Far - Near));
}

SamplerState samLinear
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};


const static int KERNEL_SIZE = 8;
const static float3 avKernel[KERNEL_SIZE] = {
	normalize( float3(  1,  1,  1 ) ) * 0.125,
	normalize( float3( -1, -1, -1 ) ) * 0.250,
	normalize( float3( -1, -1,  1 ) ) * 0.375,
	normalize( float3( -1,  1, -1 ) ) * 0.500,
	normalize( float3( -1,  1,  1 ) ) * 0.625,
	normalize( float3(  1, -1, -1 ) ) * 0.750,
	normalize( float3(  1, -1,  1 ) ) * 0.875,
	normalize( float3(  1,  1, -1 ) ) * 1.000
};

const static float2 textureSize= float2(256,256);
//float initSum = 0;
//float4 params= float4(1,0,   1, 1);


float4 psMain(vsOutput psInput) : SV_TARGET
{    
    //return float4(Passes, 0,0,1);
    //return DepthTexture.Sample(samLinear, psInput.texCoord.xy);
    float refDepth = DepthTexture.SampleLevel(samLinear, psInput.texCoord,0).r;
    float2 texelSize = float2(1.0/textureSize.x, 1.0/textureSize.y);
    float factor = 0;
    float factorIncrement = 1.0/(Passes*float(KERNEL_SIZE));
    
    //float refSceneZ = ( DepthRange.x) / (DepthRange.y + DepthRange.x - refDepth * (DepthRange.y - DepthRange.x));    
    float refSceneZ = DepthToLinear(refDepth);
    
    //return float4(refSceneZ.rrr * 2,1);

    for (int j = 0; j < Passes; j++)
    {
        float2 randomCoords= float2( fmod( psInput.texCoord.x + 1.412* j + NoiseOffset.x,1) , 
                                     fmod( psInput.texCoord.y + 1.1123* j + NoiseOffset.y,1));
        float3 random = NoiseTexture.Sample(samLinear, randomCoords).rgb;
        random = normalize(random * 2.0 - 1.0);
        for (int i = 0; i < KERNEL_SIZE; i++)
        {
            float3 vRotatedKernel = normalize(reflect( avKernel[i], random ));// * vKernelScale;
            float2 offset = vRotatedKernel.xy * KernelSize * texelSize + psInput.texCoord.xy;
            float depth = DepthTexture.SampleLevel(samLinear, offset,0).r;
                        
            //float sceneZ = ( DepthRange.x) / (DepthRange.y + DepthRange.x - depth * (DepthRange.y - DepthRange.x));    
            float sceneZ = DepthToLinear(depth);
            if (sceneZ > refSceneZ)
            {
              factor += factorIncrement;
            }
        }
    }

    float4 orgColor= ImageTexture.Sample(samLinear, psInput.texCoord);
    //return float4(factor * 0.5,0,0,1);
    
    factor = (factor - BoostShadows.x) * BoostShadows.y;
    factor = clamp(factor, 0.0, 1.0);
    float3 AOColor= lerp( Color.rgb,  float3(1,1,1), factor);   // mix shadow color
    //return float4(AOColor.rgb,1);
        
    float fadeInBackgroundFactor =    clamp(( refSceneZ - DepthClip.x ) /  (DepthClip.y - DepthClip.x), 0,1);
    
    AOColor = lerp( float3(1,1,1), AOColor, Color.a);           // fade with color Alpha    
    AOColor = lerp ( AOColor,  float3(1,1,1), (fadeInBackgroundFactor));
    
    float3 blendedColor = MixOriginal < 1 ? orgColor * MixOriginal 
                                          : orgColor + (1-orgColor) * (MixOriginal-1);
    blendedColor *= MultiplyOriginal;
    return float4( blendedColor * AOColor,1);
}












