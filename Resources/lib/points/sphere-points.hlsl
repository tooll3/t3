#include "hash-functions.hlsl"
#include "point.hlsl"


cbuffer Params : register(b0)
{
    float3 Center;
    float Radius;
    float StartAngle;
}

RWStructuredBuffer<Point> ResultPoints : u0;    // output

static float phi = PI *  (3. - sqrt(5.));  // golden angle in radians

[numthreads(256,4,1)]
void main(uint3 dtID : SV_DispatchThreadID)
{
    uint count, stride;
    ResultPoints.GetDimensions(count, stride);

    float i = dtID.x;

    float t = i / float(count - 1);
    float y = 1 - t * 2;            // y goes from 1 to -1
    float radius = sqrt(1 - y * y); // radius at y

    float theta = phi * i + StartAngle / 180 * PI;          // golden angle increment

    float x = cos(theta) * radius;
    float z = sin(theta) * radius;


    float3 position = float3(x,y,z);
    ResultPoints[dtID.x].position = position * Radius + Center;
    ResultPoints[dtID.x].w = 1;
 
    float4 rot = rotate_angle_axis( -theta, float3(0,1,0));

    // float angle2 = (2-t) * PI;
    // float angle3 = (dot( float3(0,1,0), position ) + 1) * PI / 2;
    float angle4 = atan2( y, radius)  - PI/2;
    float4 rot2 = rotate_angle_axis( angle4 , float3(0,0,1));
    
    ResultPoints[dtID.x].rotation = qmul(rot,rot2);
}

