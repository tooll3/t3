#include "lib/shared/blend-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"

cbuffer ParamConstants : register(b0)
{
    float2 Position;
    float Sides;
    float Radius; 
    float Curvature;
    float Blades;

    float Rotate;
 
    float Width;
    float Offset;
    float PingPong;
    float Repeat;

    float2 BiasAndGain;
    float BlendMode;

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

static float PI = 3.141592653;
static float TAU = (3.1415926535 * 2);

float mod(float x, float y)
{
    return (x - y * floor(x / y));
}

// based on https://www.shadertoy.com/view/Wll3R2
float sdNgon(in float2 p, in float r, in float n)
{
    // Can precompute these
    float inv_n = 1.0 / n;

    // Perform radial repeat
    float2 rp = float2(atan2(p.y, p.x), length(p)); // into polar coords
    rp.x /= TAU;
    rp.x = mod(rp.x + inv_n * 0.5, inv_n) - 0.5 * inv_n;
    rp.x *= rp.x > 0 ? (1 - saturate(Blades)) : 1;
    rp.y = saturate(lerp(rp.y, r, Curvature));
    rp.x *= TAU;

    p = float2(cos(rp.x), sin(rp.x)) * rp.y; // back to cartesian
    float2 b = float2(r, r);
    b.y = b.x * tan(TAU * inv_n * 0.5);
    float2 d = abs(p) - b;

    float sd = length(max(d, float2(0, 0))) + min(d.x, 0.0);
    return sd;
}

// based https://www.shadertoy.com/view/7tSXzt
/*float sdRegularPolygon(in float2 p, in float r, in int n )
{
    // these 4 lines can be precomputed for a given shape
    float an = 3.141593/float(n);
    float2  acs = float2(cos(an),sin(an));

    // reduce to first sector
    float bn = mod(atan2(p.x,p.y),2.0*an) - an;
    p = length(p)*float2(cos(bn),abs(sin(bn)));

    // line sdf
    p -= r*acs;
    p.y += clamp( -p.y, 0.0, r*acs.y);
    return length(p)*sign(p.x);
}*/

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
    float aspectRatio = TargetWidth / TargetHeight;
    float2 p = psInput.texCoord;
    p -= 0.5;
    p.x *= aspectRatio;

    // Rotate
    // Convert the rotation angle from degrees to radians
    float rotationRadians = radians(Rotate);
    // Apply the rotation to the point
    p = rotatePoint(p, rotationRadians);

    p += Position.xy * float2(1, -1);
    float c = sdNgon(p, Radius, Sides) * 2 - Offset * Width;
    //float c = smoothstep(Round / 2  / 4, Round / 2  / 4, d);

    float4 orgColor = ImageA.Sample(texSampler, psInput.texCoord);

    c = PingPong > 0.5
            ? (Repeat < 0.5 ? (abs(c) / Width)
                            : 1.000001 - abs(fmod(c, Width * 1.99999) - Width) / Width)
            : c / Width;

    c = Repeat > 0.5
            ? fmod(c, 1)
            : saturate(c);
    

    float dBiased = ApplyBiasAndGain(c, BiasAndGain.x, BiasAndGain.y);
    dBiased = clamp(dBiased, 0.001, 0.999);
    float4 gradient = Gradient.Sample(clammpedSampler, float2(dBiased, 0));


    return (IsTextureValid < 0.5) ? gradient : BlendColors(orgColor, gradient, (int)BlendMode);
}