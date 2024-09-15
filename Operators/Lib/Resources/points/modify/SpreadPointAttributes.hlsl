#include "shared/hash-functions.hlsl"
#include "shared/noise-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Range;
    float Phase;
}

cbuffer Params : register(b1)
{
    int Mode;
    int MappingMode;
    int ApplyMode;
}

StructuredBuffer<Point> SourcePoints : t0;
Texture2D<float4> CurveImage : register(t1);
Texture2D<float4> GradientImage : register(t2);

RWStructuredBuffer<Point> ResultPoints : u0;
sampler ClampedSampler : register(s0);

float3 fmod(float3 x, float3 y)
{
    return (x - y * floor(x / y));
}

#define SPREADMODE_BUFFER 0
#define SPREADMODE_W 1
#define SPREADMODE_SELECTION 2

#define MAPPING_NORMAL 0
#define MAPPING_FORSTART 1
#define MAPPING_PINGPONG 2
#define MAPPING_REPEAT 3
#define MAPPING_USEORIGINALW 4

#define APPLYMODE_REPLACE 0
#define APPLYMODE_MULTIPLY 1
#define APPLYMODE_ADD 2

[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    int index = i.x;
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    if (index >= pointCount)
    {
        return;
    }

    Point p = SourcePoints[index];

    if (Mode != SPREADMODE_BUFFER && (isnan(p.W)))
    {
        ResultPoints[index] = p;
        return;
    }

    float f = 0;
    float f0 = Mode == SPREADMODE_BUFFER ? (float)index / (pointCount - 1)
                                         : p.W; // Clarify: Should we clamp w before sampling?

    if (MappingMode == MAPPING_NORMAL)
    {
        f = (f0 - 0.5) / Range + 0.5 - Phase / Range;
    }
    // What does this even mean?!
    else if (MappingMode == MAPPING_FORSTART)
    {
        f = f0 / Range - Phase;
    }
    else if (MappingMode == MAPPING_PINGPONG)
    {
        f = fmod((f0 * 2 - 1 - 2 * Phase * Range) / Range, 2);
        f += -1;
        f = abs(f);
    }
    else if (MappingMode == MAPPING_REPEAT)
    {
        f = f0 / Range - 0.5 - Phase;
        f = fmod(f, 1);
    }
    else
    {
        f = p.W;
    }

    float curveValue = CurveImage.SampleLevel(ClampedSampler, float2(f, 0.5), 0).r;
    float4 gradientColor = GradientImage.SampleLevel(ClampedSampler, float2(f, 0.5), 0);

    if (!isnan(p.W))
    {
        float w = 0;
        if (ApplyMode == APPLYMODE_REPLACE)
        {
            w = curveValue;
        }
        else if (ApplyMode == APPLYMODE_MULTIPLY)
        {
            w = p.W * curveValue;
        }
        else if (ApplyMode == APPLYMODE_ADD)
        {
            w = p.W + curveValue;
        }

        p.W = lerp(p.W, w, Amount);
    }

    p.Color = p.Color * gradientColor;

    ResultPoints[index] = p;
}
