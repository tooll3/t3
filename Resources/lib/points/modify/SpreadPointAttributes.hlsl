#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float MappingMode;
    float Mode;
    float Range;
    float Phase;
}

StructuredBuffer<Point> SourcePoints : t0;        
Texture2D<float4> CurveImage : register(t1);
Texture2D<float4> GradientImage : register(t2);

RWStructuredBuffer<Point> ResultPoints : u0;
sampler texSampler : register(s0);


float3 fmod(float3 x, float3 y) {
    return (x - y * floor(x / y));
} 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    int index = i.x;
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    if(index >= pointCount) {        
        return;
    }

    Point p = SourcePoints[index];
    
    float f=0;

    if(MappingMode < 0.5 ) {
        // Normal
        f= ((float)index / pointCount - 0.5)/Range + 0.5 + Phase/Range;
    
    }
    else if(MappingMode < 1.5) {
        // From start
        f= (float)index / Range + Phase;
    }
    else if(MappingMode < 2.5) { 
        // PingPing
        f= ((float)index / pointCount) / Range - 0.5 + Phase;
        f =fmod(f,2);
        f += -1;
        f = abs(f);
        
    }
    else if(MappingMode < 3.5) { 
        // Repeat
        f= ((float)index / pointCount) / Range - 0.5 + Phase;        
        f =fmod(f,1);
    }
    else {
        // original w 
        f = p.W;
    }

    float curveValue =  CurveImage.SampleLevel(texSampler, float2(f,0.5) ,0).r;
    float4 gradientColor = GradientImage.SampleLevel(texSampler, float2(f,0.5) ,0);
    
    float w = 0;

    if(Mode < 0.5) {
        w= !isnan(p.W) ? curveValue : p.W;
    }
    else if(Mode < 1.5) {
        w= p.W * curveValue;
    }
        else if(Mode < 2.5) {
        w= p.W + curveValue;
    }

    p.W = lerp(p.W, w, Amount);;
    p.Color = p.Color * gradientColor;

    ResultPoints[index] = p;
}

