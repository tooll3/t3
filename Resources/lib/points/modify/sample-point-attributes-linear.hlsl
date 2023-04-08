#include "lib/shared/point.hlsl"

static const float4 Factors[] =
    {
        //     x  y  z  w
        float4(0, 0, 0, 0), // 0 nothing
        float4(1, 0, 0, 0), // 1 for x
        float4(0, 1, 0, 0), // 2 for y
        float4(0, 0, 1, 0), // 3 for z
        float4(0, 0, 0, 1), // 4 for w
        float4(0, 0, 0, 0), // avoid rotation effects
};

cbuffer Params : register(b0)
{
    float L;
    float LFactor;
    float LOffset;

    float R;
    float RFactor;
    float ROffset;

    float G;
    float GFactor;
    float GOffset;

    float B;
    float BFactor;
    float BOffset;

    float Mode;
    float TranslationSpace;
    float RotationSpace;
}

StructuredBuffer<Point> Points : t0;
RWStructuredBuffer<Point> ResultPoints : u0; // output

Texture2D<float4> inputTexture : register(t1);
sampler texSampler : register(s0);

[numthreads(256, 4, 1)] void main(uint3 i
                                  : SV_DispatchThreadID)
{
    uint index = i.x;

    Point P = Points[index];
    uint pointCount, stride;
    ResultPoints.GetDimensions(pointCount, stride);
    if (i.x >= pointCount)
        return;

    float divider = pointCount < 2 ? 1 : (pointCount - 1);
    float f = (float)i.x / divider;
    float2 uv = float2(f, 0.5);

    // float3 pos = P.position;
    // pos -= Center;

    // float3 posInObject = mul(float4(pos.xyz, 0), transformSampleSpace).xyz;

    float4 c = inputTexture.SampleLevel(texSampler, uv, 0);
    float gray = (c.r + c.g + c.b) / 3;

    // Rotation
    ResultPoints[index].rotation = P.rotation;

    float4 rot = P.rotation;
    float rotXFactor = (R == 5 ? (c.r * RFactor + ROffset) : 0) +
                       (G == 5 ? (c.g * GFactor + GOffset) : 0) +
                       (B == 5 ? (c.b * BFactor + BOffset) : 0) +
                       (L == 5 ? (gray * LFactor + LOffset) : 0);

    float rotYFactor = (R == 6 ? (c.r * RFactor + ROffset) : 0) +
                       (G == 6 ? (c.g * GFactor + GOffset) : 0) +
                       (B == 6 ? (c.b * BFactor + BOffset) : 0) +
                       (L == 6 ? (gray * LFactor + LOffset) : 0);

    float rotZFactor = (R == 7 ? (c.r * RFactor + ROffset) : 0) +
                       (G == 7 ? (c.g * GFactor + GOffset) : 0) +
                       (B == 7 ? (c.b * BFactor + BOffset) : 0) +
                       (L == 7 ? (gray * LFactor + LOffset) : 0);

    float tau = 3.141578 / 180;

    float4 rot2 = float4(0, 0, 0, 1);

    if (rotXFactor != 0)
    {
        rot2 = qmul(rot2, rotate_angle_axis(rotXFactor * tau, float3(1, 0, 0)));
    }
    if (rotYFactor != 0)
    {
        rot2 = qmul(rot2, rotate_angle_axis(rotYFactor * tau, float3(0, 1, 0)));
    }
    if (rotZFactor != 0)
    {
        rot2 = qmul(rot2, rotate_angle_axis(rotZFactor * tau, float3(0, 0, 1)));
    }

    rot2 = normalize(rot2);

    ResultPoints[index].rotation = qmul(rot, rot2);

    // Position
    float4 ff = Factors[(uint)clamp(L, 0, 5.1)] * (gray * LFactor + LOffset) +
                Factors[(uint)clamp(R, 0, 5.1)] * (c.r * RFactor + ROffset) +
                Factors[(uint)clamp(G, 0, 5.1)] * (c.g * GFactor + GOffset) +
                Factors[(uint)clamp(B, 0, 5.1)] * (c.b * BFactor + BOffset);

    float3 offset = Mode < 0.5 ? float3(ff.xyz)
                               : float3(ff.xyz) * P.position;

    if (TranslationSpace > 0.5)
    {
        offset = rotate_vector(offset, P.rotation);
    }

    float3 newPos = P.position + offset;

    if (RotationSpace < 0.5)
    {
        newPos = rotate_vector(newPos, rot2);
    }
    ResultPoints[index].position = newPos;

    ResultPoints[index].w = Mode < 0.5 ? (P.w + ff.w)
                                       : (P.w * (1 + ff.w));
}