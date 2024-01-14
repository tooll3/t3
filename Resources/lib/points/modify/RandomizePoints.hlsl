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

    float3 RandomizeExtend;
    float RandomSeed;


    float2 BiasAndGain;
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
    float4 biasedA = GetBiasGain(lerp(hash41u(phaseIndex ), hash41u(phaseIndex + 1), t), BiasAndGain.x, BiasAndGain.y);
    float4 biasedB = GetBiasGain(lerp(hash41u(phaseIndex + _PRIME0 ), hash41u(phaseIndex + _PRIME0 + 1), t), BiasAndGain.x, BiasAndGain.y);

    float amount = Amount * lerp( UseSelection, p.Selected,1);
    float4 rot = p.Rotation;
    
    biasedA -= OffsetMode * 0.5;
    biasedB -= OffsetMode * 0.5;

    p.Position += amount * (
        UsePointSpace == 0 
            ? qRotateVec3(biasedA.xyz * RandomizePosition, p.Rotation)
            : biasedA.xyz * RandomizePosition
        );

    
    float4 LCHa = float4( RgbToLCh(p.Color.rgb), p.Color.a);
    LCHa += biasedB * RandomizeColor * amount;
    
    float4 rgba = float4( LChToRgb(LCHa.xyz), LCHa.a);
    p.Color = ClampColorsEtc ? saturate(rgba) : rgba;

    p.W += biasedA.w * RandomizeW * amount;

    if(ClampColorsEtc && !isnan(p.W)) {
        p.W = max(0, p.W);

    }
    p.Extend += float3(biasedB.w, biasedA.w, biasedA.z) * RandomizeExtend * amount; // Not ideal... distribution overlap

    // Rotation
    float3 randomRotate = (RandomizeRotation / 180 * PI) * amount * biasedA.xyz; 
    rot = normalize(qMul(rot, qFromAngleAxis(randomRotate.x , float3(1,0,0))));
    rot = normalize(qMul(rot, qFromAngleAxis(randomRotate.y , float3(0,1,0))));
    rot = normalize(qMul(rot, qFromAngleAxis(randomRotate.z , float3(0,0,1))));
    p.Rotation = rot;

    ResultPoints[i.x] = p;
}

