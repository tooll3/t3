#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

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
        f = p.w;
    }

    float curveValue =  CurveImage.SampleLevel(texSampler, float2(f,0.5) ,0).r;
    
    float w = 0;

    if(Mode < 0.5) {
        w= !isnan(p.w) ? curveValue : p.w;
    }
    else if(Mode < 1.5) {
        w= p.w * curveValue;
    }
        else if(Mode < 2.5) {
        w= p.w + curveValue;
    }

    w = lerp(p.w, w, Amount);

    ResultPoints[index].w = w;
    ResultPoints[index].position = p.position;
    ResultPoints[index].rotation = p.rotation;
}

