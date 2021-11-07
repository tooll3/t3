#include "point.hlsl"

cbuffer Params : register(b0)
{
    float3 Count;
    float __padding1;

    float3 Size;
    float __padding3;

    float3 Center;
    float W;    

    float3 OrientationAxis;
    float OrientationAngle;

    float3 Pivot;
    float SizeMode;
}

RWStructuredBuffer<Point> ResultPoints : u0;    // output

[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint index = i.x; 

    // Note: We assume that 0 count have been clamped earlier
    uint3 c = (uint3)Count;

    uint3 cell = int3(
        index % c.x,
        index / c.x % c.y,
        index / (c.x * c.y) % c.z);

    float3 clampedCount = uint3( 
        c.x == 1 ? 1 : c.x-1,
        c.y == 1 ? 1 : c.y-1,
        c.z == 1 ? 1 : c.z-1
        );


    float3 zeroAdjustedSize = float3(
        c.x == 1 ? 0 : Size.x,
        c.y == 1 ? 0 : Size.y,
        c.z == 1 ? 0 : Size.z
    );



    float3 pos = SizeMode > 0.5 ? zeroAdjustedSize * (cell / clampedCount) - zeroAdjustedSize * (Pivot  + 0.5)
                                : zeroAdjustedSize * cell - zeroAdjustedSize * clampedCount * (Pivot  + 0.5);

    pos+= Center;
    ResultPoints[index].position = pos;
    ResultPoints[index].w = W;
    ResultPoints[index].rotation = rotate_angle_axis(OrientationAngle*PI/180, normalize(OrientationAxis));
}

