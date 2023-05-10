#include "lib/shared/point.hlsl"
#include "lib/shared/point-light.hlsl"

static const float3 Corners[] =
    {
        float3(-1, -1, 0),
        float3(1, -1, 0),
        float3(1, 1, 0),
        float3(1, 1, 0),
        float3(-1, 1, 0),
        float3(-1, -1, 0),
};


static const uint digits[5] = {
    973012991, //0b111 001 111 111 101 111 111 111 111 111 , 
    690407533, //0b101 001 001 001 101 100 100 001 101 101 , 
    704642815, //0b101 001 111 111 111 111 111 011 111 111 , 
    696556137, //0b101 001 100 001 001 001 101 001 101 001 , 
    972881535, //0b111 001 111 111 001 111 111 001 111 111   
};


cbuffer Transforms : register(b0)
{
    float4x4 CameraToClipSpace;
    float4x4 ClipSpaceToCamera;
    float4x4 WorldToCamera;
    float4x4 CameraToWorld;
    float4x4 WorldToClipSpace;
    float4x4 ClipSpaceToWorld;
    float4x4 ObjectToWorld;
    float4x4 WorldToObject;
    float4x4 ObjectToCamera;
    float4x4 ObjectToClipSpace;
};

cbuffer Params : register(b2)
{
    float4 Color;

    float Size;
    float SegmentCount;
    float CutOffTransparent;
    float FadeNearest;
    float UseWForSize;
};

cbuffer FogParams : register(b3)
{
    float4 FogColor;
    float FogDistance;
    float FogBias;
}

cbuffer PointLights : register(b4)
{
    PointLight Lights[8];
    int ActiveLightCount;
}

cbuffer RequestedResolution: register(b5) 
{
    float TargetWidth;
    float TargetHeight;
}

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
    float fog : FOG;
    int id: ID;
};

sampler texSampler : register(s0);

StructuredBuffer<Point> Points : t0;
Texture2D<float4> texture2 : register(t1);

static const int DigitCount=5;

psInput vsMain(uint id
               : SV_VertexID)
{
    psInput output;

    int quadIndex = id % 6;
    int particleId = id / 6;
    Point pointDef = Points[particleId];

    // float4 aspect = float4(CameraToClipSpace[1][1] / CameraToClipSpace[0][0],1,1,1);
    float3 quadPos = Corners[quadIndex];

    float4 posInObject = float4(pointDef.position, 1);
    //float4 quadPosInCamera = mul(posInObject, ObjectToCamera);
    float4 posInClipSpace = mul(posInObject, ObjectToClipSpace);
    posInClipSpace.xyz /= posInClipSpace.w;
    posInClipSpace.xy = floor(posInClipSpace.xy * float2(TargetWidth,TargetHeight)) / float2(TargetWidth,TargetHeight);

    float width = 4.0*DigitCount;
    float height = 7;

    float2 s = float2(width/TargetWidth, 7.0/TargetHeight);
    //float2 s = float2(0.1,0.1);
    posInClipSpace.xy += quadPos.xy * s + float2(30.0/TargetWidth,-10.0/TargetHeight);
    posInClipSpace.w=1;
    output.position = posInClipSpace;

    output.texCoord = (float2(0, 1) + (quadPos.xy * 0.5 + 0.5) * float2(1, -1))* float2(5,1);
    output.id = particleId;
    //output.position = mul(quadPosInCamera, CameraToClipSpace);
    //float4 posInWorld = mul(posInObject, ObjectToWorld);

    // Fog
    //output.fog = pow(saturate(-posInCamera.z / FogDistance), FogBias);
    return output;
}


float4 psMain(psInput input) : SV_TARGET
{
    //return float4(1,)
    float2 uv  = input.texCoord;

    // p in digit
    //float digitIndex = uv / float2(4.0*DigitCount,1);
    int2 cell = int2(uv);
    float2 posInCell = uv - cell;


    int id = input.id;

    int digitCount = log10(id);
    if(cell.x > digitCount)
        discard;

    cell.x += 5- digitCount;

    int digit = floor(id / pow(10,5-cell.x)) % 10;
    int2 pixelInCell = posInCell * int2(4,7);
    if(pixelInCell.x > 2)
        discard;
    
    //return float4(pixelInCell.x/5.0,0,0,1);

    bool bit = digits[pixelInCell.y] >> ((10-digit) * 3 - pixelInCell.x-1) & 1;

    if(!bit)
        discard;

    return Color;
    //return float4(posInCell ,digit * 10.0,1);


    return float4(uv,0,1);
    float4 textureCol = texture2.Sample(texSampler, input.texCoord);

    if (textureCol.a < CutOffTransparent)
        discard;

    float4 col = input.color * textureCol;
    col.rgb = lerp(col.rgb, FogColor.rgb, input.fog * FogColor.a);
    return clamp(col, float4(0, 0, 0, 0), float4(1000, 1000, 1000, 1));
}
