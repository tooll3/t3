#include "lib/shared/blend-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float2 Size;
    float4 CornersRadius;
    float Rotation; 
    float Width;
    float Offset;
    float PingPong;
    float Repeat;
    float BlendMode;
    float2 BiasAndGain;
    
    

    float IsTextureValid; // Automatically added by _FxShaderSetup
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
Texture2D<float4> Gradient : register(t1);
sampler texSampler : register(s0);
sampler clammpedSampler : register(s1);

float fmod(float x, float y)
{
    return (x - y * floor(x / y));
}

//source: https://iquilezles.org/articles/distfunctions2d/
float sdRoundedBox( in float2 p, in float2 b, in float4 r )
{
    r.xy = (p.x>0.0)?r.xy : r.zw;
    r.x  = (p.y>0.0)?r.x  : r.y;
    float2 q = abs(p)-b+r.x;
    return min(max(q.x,q.y),0.0) + length(max(q,0.0)) - r.x;
}

// Function to rotate a point around the origin
inline float2 rotatePoint(float2 p, float angle)
{
    float cosAngle = cos(angle);
    float sinAngle = sin(angle);
    return float2(
        p.x * cosAngle - p.y * sinAngle,
        p.x * sinAngle + p.y * cosAngle
    );
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float2 uv = psInput.texCoord;

    float aspectRation = TargetWidth / TargetHeight;
    float2 p = uv;
    p -= 0.5;
    p.x *= aspectRation;
    p+=Center * float2(-1,1);
    // Convert the rotation angle from degrees to radians
    float rotationRadians = radians(Rotation);
    // Apply the rotation to the point
    p = rotatePoint(p, rotationRadians);
    
    float c = 0;

    c = sdRoundedBox(p, Size, CornersRadius)* 2 - Offset * Width;
    
    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);

    c = PingPong > 0.5
            ? (Repeat < 0.5 ? (abs(c) / Width)
                            : 1.000001 - abs(fmod(c, Width * 1.99999) - Width) / Width)
            : c / Width;

    c = Repeat > 0.5
            ? fmod(c, 1)
            : saturate(c);

    float dBiased = ApplyBiasAndGain(c, BiasAndGain.x, BiasAndGain.y);

    //dBiased = clamp(dBiased, 0.001, 0.999); // I don't think it's needed
    float4 gradient = Gradient.Sample(clammpedSampler, float2(dBiased, 0));

    return (IsTextureValid < 0.5) ? gradient : BlendColors(orgColor, gradient, (int)BlendMode);
}