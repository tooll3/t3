
static const float3 Quad[] =
{
  float3(0, -1, 0),
  float3( 1, -1, 0),
  float3( 1,  0, 0),
  float3( 1,  0, 0),
  float3(0,  0, 0),
  float3(0, -1, 0),
};

static const float4 UV[] =
{
    //    min  max
     //   U V  U V
  float4( 1, 0, 0, 1),
  float4( 0, 0, 1, 1),
  float4( 0, 1, 1, 0),
  float4( 0, 1, 1, 0),
  float4( 1, 1, 0, 0),
  float4( 1, 0, 0, 1),
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

cbuffer Params : register(b1)
{
    float4 Color;
    float4 Shadow;
    float AnimProgress;
    float AnimSpread;
};


struct GridEntry
{
    float3 Position;     
    float Size;             // 3
    float AspectRatio;      // 4
    float4 Orientation;     // 5
    float4 Color;           // 9
    float4 UvMinMax;        // 13
    uint CharIndex;         // 17
    uint LineNumber;        // 18
    uint __padding2;
};

struct Output
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
    float4 color : COLOR;
};

StructuredBuffer<GridEntry> GridEntries : t0;
Texture2D<float4> FontTexture : register(t1);
Texture2D<float4> ColorAnim : register(t2);
Texture2D<float4> CurveAnim : register(t3);
sampler texSampler : register(s0);

/*
 Computes the sub-progress for elements of an animation that's build of multiple delayed
 animations. Progress values always normalized.

 see here https://www.desmos.com/calculator/kby3rpbize
*/
float ComputeOverlappingProgress(float normalizedProgress, int index, int count, float spread)
{
    index = count - index -1;
    normalizedProgress = 1-normalizedProgress;
    float flankLength = 1 - spread + spread / count;
    float y= normalizedProgress < 0.5 ? saturate(( normalizedProgress * 2 - (index * spread / count)) / flankLength)/2
                                      : saturate(( normalizedProgress * 2 -1 - (index * spread / count)) / flankLength)/2 + 0.5 ;
    return 1-y;
}


Output vsMain(uint id: SV_VertexID)
{
    Output output;

    uint entryCount, __;
    GridEntries.GetDimensions(entryCount, __);

    int vertexIndex = id % 6;
    int entryIndex = id / 6;

    GridEntry entry = GridEntries[entryIndex];
    float animProgress = ComputeOverlappingProgress(AnimProgress, entry.CharIndex, entryCount , AnimSpread);
    

    float3 offset = float3(        
        CurveAnim.SampleLevel(texSampler, float2(animProgress, (2.+1)/8),0).r,
        CurveAnim.SampleLevel(texSampler, float2(animProgress, (4.+1)/8),0).r,
        CurveAnim.SampleLevel(texSampler, float2(animProgress, (6.+1)/8),0).r);

    float sizeAnim = CurveAnim.SampleLevel(texSampler, float2(animProgress, 1/8*0.5),0).r;

    float3 posInObject = entry.Position + offset * float3(0,0,10) * entry.Size;

    float s = entry.Size;
    float3 quadPos = Quad[vertexIndex] * float3(1,sizeAnim,0);
    posInObject.xy += quadPos.xy * float2(s * entry.AspectRatio, s);

    float4 quadPosInWorld = mul(float4(posInObject.xyz,1), ObjectToWorld);
    float4 quadPosInCamera = mul(quadPosInWorld, WorldToCamera);
    output.position = mul(quadPosInCamera, CameraToClipSpace);
    float4 uv = entry.UvMinMax * UV[vertexIndex];
    output.texCoord =  uv.xy + uv.zw;

    
    float4 animColor = ColorAnim.SampleLevel(texSampler, float2( animProgress, 0.5 ),0);
    output.color = animColor;
    return output;
}


struct PsInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
    float4 color : COLOR;
};

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

float4 psMain(PsInput input) : SV_TARGET
{
    float3 smpl1 =  FontTexture.Sample(texSampler, input.texCoord).rgb;
    int height, width;
    FontTexture.GetDimensions(width,height);

    // from https://github.com/Chlumsky/msdfgen/issues/22#issuecomment-234958005
    float2 dx2 = abs(ddx( input.texCoord.xy ) * width);
    float2 dy2 = abs(ddy( input.texCoord.xy ) * height);
    float dx= max(dx2.x, dx2.y);
    float dy= max(dy2.x, dy2.y);
    float edge = rsqrt( dx * dx + dy * dy );

    float toPixels =  edge * 16;
    float sigDist = median( smpl1.r, smpl1.g, smpl1.b ) - 0.5;
    float letterShape = clamp( sigDist * toPixels + 0.5, 0.0, 1.0 );

    if(Shadow.a < 0.01) {
        return float4(Color.rgb, letterShape * Color.a) * input.color;// + float4(1,1,1,0.04);
    }

    float glow = pow( smoothstep(0, 1, sigDist + 0.3), 0.4);

    return float4(
        lerp(Shadow.rgb, Color.rgb, saturate(pow(letterShape,0.3)) ),
        max( saturate(letterShape*2),glow * Shadow.a) * Color.a
    );
}
