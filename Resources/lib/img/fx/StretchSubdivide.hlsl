#include "lib/shared/hash-functions.hlsl"


cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float2 Stretch2;

    float Size;
    float SubdivisionThreshold;
    float Padding;
    float Feather;

    float4 GapColor;
    float MixOriginal;
    float MaxSubdivisions;

    float Randomize;
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

Texture2D<float4> Image : register(t0);
Texture2D<float4> FxImage : register(t1);
sampler texSampler : register(s0);


#define fmod(x, y) ((x) - (y) * floor((x) / (y)))

static const float stepOffset = 0.25;

float PhaseHash(uint i) 
{
    uint pointU = i * _PRIME0;
    float particlePhaseOffset = hash11u(pointU);
    float phase = abs(particlePhaseOffset + Randomize);

    int phaseIndex = (int)phase + pointU; 

    float t = fmod (phase,1);
    t =  smoothstep(0,1,t);

    return  lerp(hash11u(phaseIndex ), 
            hash11u(phaseIndex + 1), 
            t);

}

float4 psMain(vsOutput input) : SV_TARGET
{
    float width, height;
    Image.GetDimensions(width, height);

    float2 uv = input.texCoord;
    float currentSize = Size;    
    int steps = (int)clamp(MaxSubdivisions,1,12);

    int mainSeed = 1;
    int step;

    float2 size = 1;
    float2 uvInCell = uv;
    float hash2 = 0.5;
    int seed2 = 1;

    for( step = 0; step<steps; ++step) 
    {

        if(hash11u(seed2) * 2 < size.x/size.y ) 
        {
            if(uvInCell.x < hash2 ) 
            {
                uvInCell.x /= hash2;
                size.x *= hash2;
                mainSeed += (int)(hash2+ 2123);
                seed2 *=2;
            }
            else {
                uvInCell.x = (uvInCell.x -hash2) / (1- hash2);
                size.x *= (1-hash2);
                mainSeed = (int)(mainSeed+ 213) % 1251;
                seed2 *=3;

            }
        }
        else {
            if(uvInCell.y < hash2 ) {
                uvInCell.y /= hash2;                
                size.y *= hash2;
                mainSeed = (int)(mainSeed+ 113) % 1251;
                seed2 *=5;


            }
            else {
                uvInCell.y = (uvInCell.y -hash2) / (1- hash2);
                size.y *= (1-hash2);
                mainSeed = (int)(mainSeed+ 111113) % 1251;
                seed2 *=7;
            } 
        }
        hash2 = PhaseHash(mainSeed) * 0.2 +0.4;
        if(hash11u(seed2) < 0.1)
            break;
        

    }

    float4 imageColor = Image.Sample(texSampler, uvInCell);
    imageColor.rgb *= hash41u(mainSeed).rgb;
    return imageColor;
}