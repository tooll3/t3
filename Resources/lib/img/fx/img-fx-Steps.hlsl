cbuffer ParamConstants : register(b0)
{
    float Steps;
    float Repeats;
    float Bias;
    float Offset;

    
    float4 Highlight;
    float HighlightIndex;
    float SmoothRadius;
    
    // float2 OffsetImage;
    // float __dummy__;
    // float ShadeAmount;
    // float4 ShadeColor;
    // float2 Center;
}

cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

cbuffer TimeConstants : register(b2)
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
Texture2D<float4> RampImageA : register(t1);
sampler texSampler : register(s0);

float mod(float x, float y) {
    return (x - y * floor(x / y));
} 

float Bias2(float x, float bias)
{
    return x / ((1 / bias - 2) * (1 - x) + 1);
}

float3 calcStepAndOffset(float4 orgColor) {
    float cOrg = clamp((orgColor.r + orgColor.g + orgColor.b)/3, 0.001,1);
    
    float cBiased = Bias2(cOrg, clamp(Bias, 0.005, 0.995)+1 / 2) * Repeats;    
    float tmp = cBiased + Offset/Steps  -0.001;  // avoid inpression offset
    
    float rest = mod( tmp, 1./Steps);
    float step = cBiased-rest;
    return float3(step, rest*Steps, cBiased);
} 

float4 psMain(vsOutput psInput) : SV_TARGET
{   
    //return Highlight;
    float2 p = psInput.texCoord;
    float2 res= float2(0.5/TargetWidth, 0.5/TargetHeight) * SmoothRadius;
    static float smoothExtremes = 0.5/ Steps;

    float3 sAndC=(
        calcStepAndOffset(ImageA.Sample(texSampler, p+res * float2(0,0)))*1
        +calcStepAndOffset(ImageA.Sample(texSampler, p+res * float2(1,1)))
        +calcStepAndOffset(ImageA.Sample(texSampler, p +res * float2(1,-1)))
        +calcStepAndOffset(ImageA.Sample(texSampler, p +res * float2(-1,1)))
        +calcStepAndOffset(ImageA.Sample(texSampler, p +res * float2(-1,-1)))
    )/5;

    float rampColor = mod( (1.0001  - sAndC.x - Offset/Steps),1);
    
    float extremeDarks = saturate( (sAndC.z ) * Steps + 1/Steps );
    float extremeBright = saturate( (1-sAndC.z+ 0.5/Steps ) * Steps);
    float extremes = extremeDarks * extremeBright;
    extremes = 1;

    float4 colorFromRamp= RampImageA.Sample(texSampler, float2( rampColor ,0.5/2));    
    if((int)(rampColor * Steps) == (int)((HighlightIndex +0.01) % Steps) ) {
        colorFromRamp.rgb = lerp(colorFromRamp.rgb, Highlight.rgb, Highlight.a);
    }

    float4 colorFromEdge= RampImageA.Sample(texSampler, float2(sAndC.y* extremes , 1.5/2));
    float a = clamp(colorFromRamp.a + colorFromEdge.a - colorFromRamp.a*colorFromEdge.a, 0,1);
    float3 rgb = (1.0 - colorFromEdge.a)*colorFromRamp.rgb + colorFromEdge.a*colorFromEdge.rgb;   
    return float4(rgb,a);
}