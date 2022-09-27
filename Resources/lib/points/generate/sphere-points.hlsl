#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"


cbuffer Params : register(b0)
{
    float3 Center;
    float Radius;
    float StartAngle;
    float Scatter;
}

RWStructuredBuffer<Point> ResultPoints : u0;    // output


static float precision = 0.0001;
static float phi = PI *  (3. - sqrt(5.));  // golden angle in radians
static float modPi = 2*PI * precision;
static float toRad = PI/180;

// Fix orientation so z aligns with sphere normal
static float4 rot4 = rotate_angle_axis( -PI/2 , float3(1,0,0));    
static float4 rot5 = qmul(rot4,  rotate_angle_axis( PI/2 , float3(0,0,1)));    

[numthreads(256,4,1)]
void main(uint3 dtID : SV_DispatchThreadID)
{
    uint count, stride;
    ResultPoints.GetDimensions(count, stride);

    float i = dtID.x;

    float t = i / float(count - 1);
    float y = 1 - t * 2;            // y goes from 1 to -1
    float radius = sqrt(1 - y * y); // radius at y

    float theta = i*precision;
    theta *= phi;
    theta %= modPi;
    theta /= precision;
    theta += StartAngle * toRad;
    theta += (hash11(i/123.71) -0.5) * Scatter;
    
    //float theta = (phi * i) + StartAngle / 180 * PI;          // golden angle increment

    float x = cos(theta) * radius;
    float z = sin(theta) * radius;


    float3 position = float3(x,y,z);
    ResultPoints[dtID.x].position = position * Radius + Center;
    ResultPoints[dtID.x].w = 1;


    float4 rot = rotate_angle_axis( theta, float3(0,-1,0));
    float angle4 = atan2( y, radius)  - PI/2;
    float4 rot2 = rotate_angle_axis( angle4 , float3(0,0,1));    
    ResultPoints[dtID.x].rotation = qmul( qmul(rot,rot2) , rot5);
}

