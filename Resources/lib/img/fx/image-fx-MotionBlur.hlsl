//#include "hash-functions.hlsl"
#include "point.hlsl"

cbuffer ParamConstants : register(b0)
{
    float DisplaceAmount;
    float DisplaceOffset;
    float Twist;
    float Shade;

    float SampleCount;
    float SampleRadius;
    float SampleSpread;
    float SampleOffset;

    float4x4 WorldToCameraMatrix;
    float4x4 CameraToClipSpaceMatrix;
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

Texture2D<float4> Image : register(t0);
Texture2D<float4> DepthMap : register(t1);
sampler texSampler : register(s0);


float IsBetween( float value, float low, float high) {
    return (value >= low && value <= high) ? 1:0;
}


float4 psMain(vsOutput psInput) : SV_TARGET
{   
    int samples = (int)clamp(SampleCount+0.5,1,32);
    float displaceMapWidth, displaceMapHeight;

    float2 uv = psInput.texCoord;
    float4 c= DepthMap.Sample(texSampler, uv);    

    float depth = DepthMap.Sample(texSampler, uv).r;
    depth = min( depth, 0.999);
    return float4(1,1,0,1);

    float4 viewTFragPos = float4(-uv.x*2.0 + 1.0, uv.y*2.0 - 1.0, depth, 1.0);
    float4 worldTFragPos = mul(viewTFragPos, ClipSpaceToWorldMatrix);  // viewToWorld?
    worldTFragPos /= worldTFragPos.w;

    // float4 viewTPreviousFragPos = mul(worldTFragPos, previousWorldToView);
    // viewTPreviousFragPos /= viewTPreviousFragPos.w;
  
    // float2 velocity = (viewTFragPos.xy - viewTPreviousFragPos.xy)*Strength;
    // velocity.x = -velocity.x;
    // if (abs(velocity.x) < 0.0001)
    //     velocity.x = 0.0;
    // if (abs(velocity.y) < 0.0001)
    //     velocity.y = 0.0;

    // float l = length(velocity);
    // if (l > 0 && l > Clamp_)
    //     velocity *= Clamp_/l;

    // float2 dir = velocity*10.0/NumberOfSamples;
    // float2 pos = dir;
    // float totalWeight = 1;
    
    // float weight=1;
    // for (int i = 0; i < NumberOfSamples; ++i)
    // {
    //     c += Image.SampleLevel(texSampler, uv + pos, 0)*weight;
    //     c += Image.SampleLevel(texSampler, uv - pos, 0)*weight;
    //     pos += dir;
    //     totalWeight += 2*weight;
    // }
    // c.rgb /= totalWeight;
    // c.a = 1.0;

    // return c;


}