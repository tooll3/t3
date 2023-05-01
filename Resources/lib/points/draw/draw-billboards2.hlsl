#include "lib/shared/point.hlsl"
#include "lib/shared/hash-functions.hlsl"

static const float3 Corners[] =
    {
        float3(-1, -1, 0),
        float3(1, -1, 0),
        float3(1, 1, 0),
        float3(1, 1, 0),
        float3(-1, 1, 0),
        float3(-1, -1, 0),
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
    float2 Stretch;
    float2 Offset;

    float Scale;
    float OrientationMode;
    float Rotate;
    float Randomize;

    float RandomPhase;
    float3 RandomPosition;

    float RandomRotate;
    float RandomScale;
    float2 __padding0;

    float4 Color;

    float ColorVariationMode;
    float ScaleDistribution;
    float SpreadLength;
    float SpreadPhase;

    float SpreadPingPong;
    float SpreadRepeat;
    float2 AtlasSize;

    float TextureAtlasMode;
    float FxTextureMode;
    float AlphaCutOff;
    float IsFxTextureConnected;

    // float __padding1; // order adjusted for 4x4 padding

    float4 FxTextureAmount;
};

cbuffer FogParams : register(b2)
{
    float4 FogColor;
    float FogDistance;
    float FogBias;
}

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
    float fog : FOG;
};

sampler texSampler : register(s0);

StructuredBuffer<Point> Points : t0;
Texture2D<float4> SpriteTexture : register(t1);
Texture2D<float4> FxTexture : register(t2);
Texture2D<float4> ColorOverW : register(t3);
Texture2D<float4> SizeOverW : register(t4);

inline float fmod(float x, float y)
{
    return (x - y * floor(x / y));
}

inline float GetUFromMode(float mode, float f, float3 scatter, float w, float fog)
{
    switch ((int)(mode + 0.5))
    {

    case 0:
        return scatter.x;

    case 1:
        float f1 = (f + SpreadPhase) / SpreadLength;
        f1 = SpreadRepeat > 0.5 ? fmod(f1, 1) : f1;
        // float ff = SpreadPingPong > 0.5 ? abs(fmod(f1 * 2, 2) - 1) : 1;
        return SpreadPingPong > 0.5 ? (1 - abs(f1 * 2 - 1)) : f1;

        // return SpreadRepeat > 0.5 ? fmod(ff * SpreadLength + SpreadPhase, 1)
        //                           : ff * SpreadLength + SpreadPhase;

    case 2:
        return w;

    default:
        return fog;
    }
}

psInput vsMain(uint id
               : SV_VertexID)
{
    uint particleCount, stride;
    Points.GetDimensions(particleCount, stride);

    uint width, height, mips;
    SpriteTexture.GetDimensions(0, width, height, mips);
    float2 textureAspect = width > height ? float2((float)width / height, 1)
                                          : float2(1, (float)height / width);

    psInput output;

    int quadIndex = id % 6;
    int particleId = id / 6;
    float3 cornerFactors = Corners[quadIndex];

    Point p = Points[particleId];
    float f = particleId / (float)particleCount;

    float phase = RandomPhase + 133.1123 * f;
    int phaseId = (int)phase;

    float3 normalizedScatter = lerp(hash31(particleId % 123121 + phaseId),
                                    hash31(particleId % 123121 + phaseId + 1),
                                    smoothstep(0, 1,
                                               phase - phaseId));
    float3 scatterForScale = normalizedScatter * 2 - 1;

    // float4 aspect = float4(CameraToClipSpace[1][1] / CameraToClipSpace[0][0],1,1,1);

    float cellIndex = particleId;
    float textureCelX = (float)cellIndex % (AtlasSize.x);
    float textureCelY = (int)(((float)cellIndex / AtlasSize.x) % (float)(AtlasSize.y));
    output.texCoord = (cornerFactors.xy * float2(-1, 1) * 0.5 + 0.5);
    output.texCoord /= AtlasSize;
    output.texCoord += float2(textureCelX, textureCelY) / AtlasSize;

    float4 posInObject = float4(p.position, 1);

    float3 randomOffset = rotate_vector((normalizedScatter - 0.5) * 2 * RandomPosition * Randomize, p.rotation);
    posInObject += float4(randomOffset, 0);

    // float3 axis = rotate_vector(p.position, rotation) * Size * sizeFromW;

    float4 quadPosInCamera = mul(posInObject, ObjectToCamera);

    // Shrink too close particles
    float4 posInCamera = mul(posInObject, ObjectToCamera);
    float tooCloseFactor = saturate(-posInCamera.z / 0.1 - 1);

    output.fog = pow(saturate(-posInCamera.z / FogDistance), FogBias);

    float scaleFxU = GetUFromMode(ScaleDistribution, f, normalizedScatter, p.w, output.fog);
    float sizeFromW = SizeOverW.SampleLevel(texSampler, float2(scaleFxU, 0), 0);
    float hideUndefinedPoints = isnan(p.w) ? 0 : 1;
    float computedScale = Scale * (RandomScale * scatterForScale.y + 1) * tooCloseFactor * sizeFromW * hideUndefinedPoints;

    if (OrientationMode <= 1.5)
    {
        float2 corner = float2((cornerFactors.xy + Offset) * 0.010 * Stretch * textureAspect) * float2(-1, -1);

        float4 rot = rotate_angle_axis((Rotate + RandomRotate * scatterForScale.x) * 3.141578 / 180, float3(0, 0, 1));

        if ((int)OrientationMode == 1)
        {
            float3 yRotated = rotate_vector(float3(0, 1, 0), p.rotation);
            float4 yRotatedInCam = mul(float4(yRotated, 1), ObjectToCamera);
            float a = atan2(yRotatedInCam.x, yRotatedInCam.y);
            float4 xx = rotate_angle_axis(-a, float3(0, 0, 1));
            rot = qmul(xx, rot);
        }

        corner = rotate_vector(float3(corner, 0), rot);
        // quadPosInCamera.xy += corner * computedScale;
        output.position = mul(quadPosInCamera + float4(corner * computedScale, 0, 0), CameraToClipSpace);
    }
    else
    {
        float3 axis = float3((cornerFactors.xy + Offset) * 0.010 * Stretch * textureAspect, 0);
        float4 rotation = qmul(normalize(p.rotation), rotate_angle_axis((Rotate + 180 + RandomRotate * scatterForScale.x) / 180 * PI, float3(0, 0, 1)));
        axis = rotate_vector(axis, rotation) * computedScale;
        // float3 pInObject = p.position + axis;
        output.position = mul(posInObject + float4(axis, 0), ObjectToClipSpace);
    }

    // TODO: use effect texture
    float4 colorFromPoint = 1; //((int)UsePointOrientation == 2) ? p.rotation : 1;

    float colorFxU = GetUFromMode(ColorVariationMode, f, normalizedScatter, p.w, output.fog);
    output.color = Color * ColorOverW.SampleLevel(texSampler, float2(colorFxU, 0), 0) * colorFromPoint;

    if (IsFxTextureConnected)
    {
        float4 centerPos = mul(float4(quadPosInCamera.xyz, 1), CameraToClipSpace);
        centerPos.xyz /= centerPos.w;

        float4 fxColor = FxTexture.SampleLevel(texSampler, (centerPos.xy * float2(1, -1) + 1) / 2, 0);
        output.color *= fxColor;
    }

    return output;
}

float4 psMain(psInput input) : SV_TARGET
{
    float4 imgColor = SpriteTexture.Sample(texSampler, input.texCoord);
    float4 color = input.color * imgColor;

    if (color.a < AlphaCutOff)
        discard;

    color.rgb = lerp(color.rgb, FogColor.rgb, input.fog * FogColor.a);
    return clamp(color, 0, float4(100, 100, 100, 1));
}
