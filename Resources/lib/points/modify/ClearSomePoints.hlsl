#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/color-functions.hlsl"

cbuffer Params : register(b0)
{
    float Ratio;
}

cbuffer Params : register(b1)
{
    int Seed;
    int Repeat;
    int Resolution;
}

StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;    

inline int Mod(int val, int repeat)
{
    int x = val % repeat;
    if (x < 0)
        x = repeat + x;
    
    return x;
}

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);

    uint pointU = ((i.x - Mod(i.x, Resolution)  + 1) * _PRIME0 + Seed * _PRIME1) % (Repeat == 0 ? 999999999 : Repeat);
    float hash = hash11u(pointU );
    
    Point p = SourcePoints[i.x];
    if(hash <= Ratio) 
    {
        p.W = NAN;
    }
    ResultPoints[i.x] = p;
}

