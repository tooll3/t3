#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float Mode;
    float Amount;
    float2 ScaleUV;

    float3 Distribution;
    float UseVertexSelection;

    float3 MainOffset;
}

StructuredBuffer<PbrVertex> SourceVertices : t0;        
Texture2D<float4> DisplaceMap : register(t1);

RWStructuredBuffer<PbrVertex> ResultVertices : u0;   

sampler texSampler : register(s0);

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    uint pointCount, _;
    SourceVertices.GetDimensions(pointCount, _);
    if(gi >= pointCount) {
        return;
    }

    PbrVertex v = SourceVertices[gi];
    ResultVertices[gi] = SourceVertices[gi];

    float weight = 1;

    float3 posInWorld = v.Position;
 
    float2 uv =SourceVertices[gi].TexCoord * ScaleUV;
    float4 texColor = DisplaceMap.SampleLevel(texSampler, uv, 0); 
    float3x3 TBN = float3x3(v.Tangent, v.Bitangent, v.Normal);
    
    float3 offset = 0;
    if(Mode < 0.5) 
    {
        offset = mul(
                (   
                    (texColor.r + texColor.g + texColor.b)/3 * texColor.a * float3(0,0,1) * Distribution 
                    + MainOffset
                ) * Amount
        ,TBN);
    }
    else if(Mode< 1.5) 
    {
        offset= mul((texColor.rgb * texColor.a * Distribution + MainOffset) * Amount, TBN);
    }
    else {
        offset= (texColor.rgb * texColor.a * Distribution + MainOffset)  * Amount;
    } 

    ResultVertices[gi].Position = v.Position + offset;
}

