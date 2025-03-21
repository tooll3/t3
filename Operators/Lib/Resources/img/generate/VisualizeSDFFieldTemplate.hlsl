cbuffer ParamConstants : register(b0)
{
    float4x4 TransformMatrix;
    float4 Color;
}


cbuffer Transforms : register(b1)
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
}


cbuffer Params : register(b2)
{
    /*{FLOAT_PARAMS}*/
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
    float3 posInWorld : POSITION;
    float distToCamera : DEPTH;
    // float3 worldTViewDir : TEXCOORD1;
    // float3 worldTViewPos : TEXCOORD2;
};

static const float3 Quad[] =
    {
        float3(-1, -1, 0),
        float3(1, -1, 0),
        float3(1, 1, 0),
        float3(1, 1, 0),
        float3(-1, 1, 0),
        float3(-1, -1, 0),
};

vsOutput vsMain4(uint vertexId : SV_VertexID)
{
    vsOutput output;
    float2 quadVertex = Quad[vertexId].xy;
    float2 quadVertexInObject = quadVertex;
    float4 posInObj = mul(float4(quadVertexInObject, 0, 1), TransformMatrix);

    output.position = mul(posInObj, ObjectToClipSpace);
    output.posInWorld = mul(posInObj, ObjectToWorld).xyz;
    output.texCoord = quadVertex * float2(0.5, -0.5) + 0.5;
    // output.distToCamera = mul
    return output;
}

//---- Field functions --------------------------
/*{FIELD_FUNCTIONS}*/

//---------------------------------------
float GetDistance(float3 pos)
{
    return /*{FIELD_CALL}*/ 0;
}
//---------------------------------------------------

inline float fmod(float x, float y)
{
    return (x - y * floor(x / y));
}

#define PI acos(-1.)
#define INFINITY pow(2., 8.)

float3 fusion(float x)
{
    float t = clamp(x, 0.0, 1.0);
    return clamp(float3(sqrt(t), t * t * t, max(sin(PI * 1.75 * t), pow(t, 12.0))), 0.0, 1.0);
}

float3 FusionHDR(float x)
{
    float t = clamp(x, 0.0, 1.0);
    return fusion(sqrt(t)) * (0.5 + 2. * t);
}

float3 DistanceMeter(float dist, float rayLength, float3 rayDir, float camHeight)
{
    // float idealGridDistance = 20.0 / rayLength * pow(abs(rayDir.y), 0.8);
    float idealGridDistance = camHeight;
    float nearestBase = floor(log(idealGridDistance) / log(10.0));
    float relativeDist = abs(dist / camHeight);

    float largerDistance = pow(10.0, nearestBase + 1.0);
    float smallerDistance = pow(10.0, nearestBase);

    float3 col = FusionHDR(log(1.0 + relativeDist));
    col = max(float3(0.0, 0.0, 0.0), col);
    if (sign(dist) < 0.0)
    {
        col = col.grb * 3.0;
    }

    float l0 = pow(0.5 + 0.5 * cos(dist * 3.14159265359 * 2.0 * smallerDistance), 10.0);
    float l1 = pow(0.5 + 0.5 * cos(dist * 3.14159265359 * 2.0 * largerDistance), 10.0);

    float x = frac(log(idealGridDistance) / log(10.0));
    l0 = lerp(l0, 0.0, smoothstep(0.5, 1.0, x));
    l1 = lerp(0.0, l1, smoothstep(0.0, 0.5, x));

    col.rgb *= 0.1 + 0.9 * (1.0 - l0) * (1.0 - l1);
    return col;
}

float4 psMain(vsOutput input) : SV_TARGET
{
    float d = GetDistance(input.posInWorld);
    float2 s = float2(ddx(input.texCoord.x), ddy(input.texCoord.y));
    float3 camPos = mul(float4(0, 0, 0, 1), CameraToWorld).xyz;
    float dToCam = length(camPos - input.posInWorld);

    return float4(DistanceMeter(d, 0.1, float3(1, 1, 0), 10 / dToCam), 1) * Color;

    float2 c = fmod(d * (-s.y) * 1000, 1) < 0.05 ? 1 : 0;
    return float4(c.xx, dToCam, 1) * Color;
}
