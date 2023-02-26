#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float P;
    float Q;
    float __padding1;
    float __padding2;

    float3 Center;
    float W;
    float3 OrientationAxis;
    float OrientationAngle;

    float AMag;
    float AAng;
    float BMag;
    float BAng;
    float R;
    float Scale;
    float Offset;
}

RWStructuredBuffer<Point> ResultPoints : u0; // output

static const float ToRad = 3.141578 / 180;

[numthreads(256, 1, 1)] void main(uint3 Di
                                  : SV_DispatchThreadID)
{
    uint count, stride;

    ResultPoints.GetDimensions(count, stride);

    int index = Di.x;
    int p = (int)(P + 0.5);

    int _i = index % p; // (P - 1);
    int _j = index / p;

    float i = _i;
    float j = _j - Offset;

    float scale = Scale;
    float ang = AAng * i + BAng * j;
    float mag = pow(AMag, i) * pow(BMag, j) * scale;
    float x = cos(ang) * mag;
    float y = sin(ang) * mag;
    float radius = mag * R * 100;
    float3 pos = float3(x, y, 0);

    pos += Center;
    ResultPoints[index].position = pos;
    ResultPoints[index].w = radius * W * 1.0; // Magic offset to let points touch?

    float4 rot = rotate_angle_axis(OrientationAngle * PI / 180, normalize(OrientationAxis));
    rot = qmul(rot, rotate_angle_axis(ang, float3(0, 0, 1)));
    ResultPoints[index].rotation = rot; // rotate_angle_axis(10, float3(0, 0, 1));
    //  return;
}
