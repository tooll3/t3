/*

This shader is not intended to perform a physically accurate simulation. Instead,
it employs a stylistic approach to control the visual appearance of points using a
set of control points. For computing alignment and position, we calculate a gravity
vector, which is then used for orientation and offset.

Blending point attributes using gravity can be challenging because the gravitational
effect increases dramatically at close distances and diminishes quickly over longer
distances. To manage this, we use smoothed linear distance weights instead.

The meaningful blending range for distances is controlled by the Length and Phase parameters.
*/

#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float ColorDistance;
    float Range;
    float Phase;
}

cbuffer Params : register(b1)
{
    int FieldCount;
    int Mode;
    int MappingMode;
    int ApplyMode;
}

StructuredBuffer<Point> SourcePoints : t0;
StructuredBuffer<Point> FieldPoints : t1;

Texture2D<float4> CurveImage : register(t2);
Texture2D<float4> GradientImage : register(t2);

RWStructuredBuffer<Point> ResultPoints : u0;

sampler texSampler : register(s0);

float3 fmod(float3 x, float3 y)
{
    return (x - y * floor(x / y));
}

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

    float3 totalForce;
    float totalWeight = 0;

    float4 resultColor = float4(0, 0, 0, 0);

    for (int fieldIndex = 0; fieldIndex < FieldCount; fieldIndex++)
    {
        float3 fPos = FieldPoints[fieldIndex].Position;
        float3 dir = (p.Position - fPos) * FieldPoints[fieldIndex].W;
        float len = length(dir);
        float dd = 1 / (len + 0.1);

        float weight = smoothstep(ColorDistance, 0, len);
        totalWeight += weight;
        resultColor += max(0, FieldPoints[fieldIndex].Color) * weight;

        float distanceSq = dot(dir, dir);
        if (distanceSq > 0.0001)
        {
            // Compute the gravitational force vector (without mass, as it's constant)
            float invDistance = rsqrt(distanceSq);
            float3 force = dir * invDistance * invDistance * invDistance; // r / |r|^3
            totalForce += force;
        }
    }

    float gMagnitude = length(totalForce) + 0.0001;

    // Offset
    float3 dir = totalForce / gMagnitude;
    p.Position -= dir * clamp(gMagnitude, 0, 1) * Amount;

    // Orient towards
    p.Rotation = qLookAt(dir, float3(0, 1, 0));

    // Scale
    p.W += totalWeight * Amount * 2;

    if (totalWeight > 0.0f)
    {
        resultColor /= totalWeight;
        p.Color = lerp(p.Color, resultColor, saturate(totalWeight));
    }

    ResultPoints[index] = p;
}
