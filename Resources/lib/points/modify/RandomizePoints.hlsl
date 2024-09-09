#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/bias-functions.hlsl"
#include "lib/shared/color-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float3 RandomizePosition;

    float3 RandomizeRotation;
    float RandomizeW;

    float4 RandomizeColor;

    float3 Stretch;
    float RandomSeed;


    float2 GainAndBias;
    float UseSelection;

}
 
cbuffer IntParams : register(b1) 
{
    uint OffsetMode;
    uint UsePointSpace;
    uint Interpolation;
    int ClampColorsEtc;
    int Repeat;
}

StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;  

float3 hsb2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z < 0.5 ?
                     // float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
               c.z * 2 * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y)
                     : lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), lerp(c.y, 0, (c.z * 2 - 1)));
}

float3 rgb2hsb(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(
        abs(q.z + (q.w - q.y) / (6.0 * d + e)),
        d / (q.x + e),
        q.x * 0.5);
}

 
[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    Point p = SourcePoints[i.x];

    uint pointId = i.x;
    uint pointU = pointId * _PRIME0 % (Repeat == 0 ? 999999999 : Repeat) ;
    float particlePhaseOffset = hash11u(pointU);

    float phase = abs(particlePhaseOffset + RandomSeed);

    int phaseIndex = (uint)phase + pointU; 

    float t = fmod (phase,1);
    t = Interpolation == 0 ? 0 : (Interpolation == 1 ? t : smoothstep(0,1,t));
    float4 biasedA = ApplyBiasAndGain(lerp(hash41u(phaseIndex ), hash41u(phaseIndex + 1), t), GainAndBias.x, GainAndBias.y);
    float4 biasedB = ApplyBiasAndGain(lerp(hash41u(phaseIndex + _PRIME0 ), hash41u(phaseIndex + _PRIME0 + 1), t), GainAndBias.x, GainAndBias.y);

    float amount = Amount * lerp( UseSelection, p.Selected,1);
    float4 rot = p.Rotation;
    
    biasedA -= OffsetMode * 0.5;
    biasedB -= OffsetMode * 0.5;

    p.Position += amount * (
        UsePointSpace == 0 
            ? qRotateVec3(biasedA.xyz * RandomizePosition, p.Rotation)
            : biasedA.xyz * RandomizePosition
        );

    
    float4 HSBa = float4( rgb2hsb(p.Color.rgb), p.Color.a);
    HSBa += biasedB * RandomizeColor * amount;
    
    float4 rgba = float4( hsb2rgb(HSBa.xyz), HSBa.a);
    p.Color = ClampColorsEtc ? saturate(rgba) : rgba;

    p.W += biasedA.w * RandomizeW * amount;

    if(ClampColorsEtc && !isnan(p.W)) {
        p.W = max(0, p.W);

    }
    p.Stretch += float3(biasedB.w, biasedA.w, biasedA.z) * Stretch * amount; // Not ideal... distribution overlap

    // Rotation
    float3 randomRotate = (RandomizeRotation / 180 * PI) * amount * biasedA.xyz; 
    rot = normalize(qMul(rot, qFromAngleAxis(randomRotate.x , float3(1,0,0))));
    rot = normalize(qMul(rot, qFromAngleAxis(randomRotate.y , float3(0,1,0))));
    rot = normalize(qMul(rot, qFromAngleAxis(randomRotate.z , float3(0,0,1))));
    p.Rotation = rot;

    ResultPoints[i.x] = p;
}

