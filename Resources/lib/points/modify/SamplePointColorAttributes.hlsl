#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
//#include "lib/shared/blend-functions.hlsl"


cbuffer Params : register(b0)
{
    float4x4 transformSampleSpace;

    float3 Center;
    float Mode;
    float4 BaseColor;
    float Mix;
      
}

StructuredBuffer<Point> Points : t0;
RWStructuredBuffer<Point> ResultPoints : u0; // output

Texture2D<float4> inputTexture : register(t1);
sampler texSampler : register(s0);

[numthreads(256, 4, 1)] void main(uint3 i
                                  : SV_DispatchThreadID)
{
    uint pointCount, stride;
    ResultPoints.GetDimensions(pointCount, stride);
    if(i.x >= pointCount) {
        return;
    }

    uint index = i.x;
    

    Point p = Points[index];

    float3 pos = p.Position;
    pos -= Center;

    float3 posInObject = mul(float4(pos.xyz, 0), transformSampleSpace).xyz;
    float4 c = inputTexture.SampleLevel(texSampler, posInObject.xy * float2(1, -1) + float2(0.5, 0.5), 0.0);
    c *=BaseColor;

    float4 tA = p.Color;
    float4 tB = c;
    float gray = (c.r + c.g + c.b) / 3;
    float3 rgbNormalBlended = (1.0 - tB.a) * tA.rgb + tB.a * tB.rgb;
    if (Mode < 0.5){
        p.Color = float4(rgbNormalBlended,1);
    }
    else if (Mode < 1.5){
        p.Color = float4(1 - (1 - tA.rgb) * (1 - tB.rgb * tB.a),1);
    }
    else if (Mode < 2.5){
        c = float4(lerp(tA.rgb, tA.rgb * tB.rgb, tB.a),1);
        p.Color = c;
         
    }
    

                                     
    ResultPoints[index] = p;
}