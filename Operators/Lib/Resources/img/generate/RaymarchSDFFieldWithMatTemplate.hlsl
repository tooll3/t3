#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/point-light.hlsl"
#include "shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    /*{FLOAT_PARAMS}*/
}

cbuffer ParamConstants : register(b1)
{
    float MaxSteps;
    float StepSize;
    float MinDistance;
    float MaxDistance;

    float Fog;
    float DistToColor;
    float AODistance;
    float __padding1;

    float4 Specular;
    float4 Glow;
    float4 AmbientOcclusion;
    float4 Background;

    float3 LightPos;
    float __padding;

    float2 Spec;
}

cbuffer Transforms : register(b2)
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

// Context C Buffers
cbuffer FogParams : register(b3)
{
    float4 FogColor;
    float FogDistance;
    float FogBias;
}

cbuffer PointLights : register(b4)
{
    PointLight Lights[8];
    uint ActiveLightCount;
}

cbuffer PbrParams : register(b5)
{
    float4 BaseColor;
    float4 EmissiveColor;
    float Roughness;
    float Specular2;
    float Metal;
}

Texture2D<float4> BaseColorMap : register(t0);
Texture2D<float4> EmissiveColorMap : register(t1);
Texture2D<float4> RSMOMap : register(t2);
Texture2D<float4> NormalMap : register(t3);
Texture2D<float4> BRDFLookup : register(t4);
TextureCube<float4> PrefilteredSpecular : register(t5);

sampler texSampler : register(s0);
sampler clampedSampler : register(s1);
//--------------------

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
    float3 viewDir : VPOS;
    float3 worldTViewDir : TEXCOORD1;
    float3 worldTViewPos : TEXCOORD2;
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
    float4 quadPos = float4(Quad[vertexId], 1);
    float2 texCoord = quadPos.xy * float2(0.5, -0.5) + 0.5;
    output.texCoord = texCoord;
    output.position = quadPos;
    float4x4 ViewToWorld = ClipSpaceToWorld; // CameraToWorld ;

    float4 viewTNearFragPos = float4(texCoord.x * 2.0 - 1.0, -texCoord.y * 2.0 + 1.0, 0.0, 1.0);
    float4 worldTNearFragPos = mul(viewTNearFragPos, ViewToWorld);
    worldTNearFragPos /= worldTNearFragPos.w;

    float4 viewTFarFragPos = float4(texCoord.x * 2.0 - 1.0, -texCoord.y * 2.0 + 1.0, 1.0, 1.0);
    float4 worldTFarFragPos = mul(viewTFarFragPos, ViewToWorld);
    worldTFarFragPos /= worldTFarFragPos.w;

    output.worldTViewDir = normalize(worldTFarFragPos.xyz - worldTNearFragPos.xyz);
    output.worldTViewPos = worldTNearFragPos.xyz;

    output.viewDir = -normalize(float3(CameraToWorld._31, CameraToWorld._32, CameraToWorld._33));

    return output;
}

//--- Field functions -----------------------
/*{FIELD_FUNCTIONS}*/
//-------------------------------------------

float GetDistance(float3 pos)
{
    // pos = mul(float4(pos.xyz,1), ObjectToWorld).xyz;
    return /*{FIELD_CALL}*/ 0;
}
//---------------------------------------------------

// Blinn-Phong shading model with rim lighting (diffuse light bleeding to the other side).
// |normal|, |view| and |light| should be normalized.
float3 ComputedShadedColor(float3 normal, float3 view, float3 light, float3 diffuseColor)
{
    float3 halfLV = normalize(light + view);
    float clampedSpecPower = max(Spec.y, 0.001);
    float spe = pow(max(dot(normal, halfLV), Spec.x), clampedSpecPower);
    float dif = dot(normal, light) * 0.1 + 0.15;
    return dif * diffuseColor + spe * Specular.rgb;
}

float3 GetNormal(float3 p, float offset)
{
    float dt = .0001;
    float3 n = float3(GetDistance(p + float3(dt, 0, 0)),
                      GetDistance(p + float3(0, dt, 0)),
                      GetDistance(p + float3(0, 0, dt))) -
               GetDistance(p);
    return normalize(n);
}

float ComputeAO(float3 aoposition, float3 aonormal, float aodistance, float aoiterations, float aofactor)
{
    float ao = 0.0;
    float k = aofactor;
    aodistance /= aoiterations;
    for (int i = 1; i < 4; i += 1)
    {
        ao += (i * aodistance - GetDistance(aoposition + aonormal * i * aodistance)) / pow(2, i);
    }
    return 1.0 - k * ao;
}

static float MAX_DIST = 300;

float DepthFromWorldSpace(float distFromCamera, float nearPlane, float farPlane)
{
    // Convert a world-space distance to a 0..1 depth.
    // Assumes a linear mapping from nearPlane..farPlane -> 0..1
    return saturate((distFromCamera - nearPlane) / (farPlane - nearPlane));
}

float DepthFromWorldSpace2(float dist, float near, float far)
{
    // Convert a world-space distance to a 0..1 depth.
    // Assumes a linear mapping from nearPlane..farPlane -> 0..1
    // return saturate((distFromCamera - nearPlane) / (farPlane - nearPlane));
    return far * (dist - near) / (dist * (far - near));
}

// float4 psMain(vsOutput input) : SV_TARGET

struct PSOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

PSOutput psMain(vsOutput input)
{
    float3 eye = input.worldTViewPos;

    // Early test. This will lead to z-problems later
    // eye = mul(float4(eye,1), ObjectToWorld).xyz;
    float3 p = eye;
    float3 tmpP = p;
    float3 dp = normalize(input.worldTViewDir);
    // dp = mul(float4(dp,0), ObjectToWorld).xyz;

    float totalD = 0.0;
    float D = 3.4e38;
    D = StepSize;
    float extraD = 0.0;
    float lastD;
    int steps;
    int maxSteps = (int)(MaxSteps - 0.5);

    // Simple iterator
    for (steps = 0; steps < maxSteps && abs(D) > MinDistance; steps++)
    {
        D = GetDistance(p);
        p += dp * D;
    }

    p += totalD * dp;

    // Color the surface with Blinn-Phong shading, ambient occlusion and glow.
    float3 col = Background.rgb;
    float a = 1;

    float3 normal = 0;

    // We've got a hit or we're not sure.
    if (D < MAX_DIST)
    {
        normal = normalize(GetNormal(p, D));

        col = Specular.rgb;
        // col = ComputedShadedColor(n, -dp, LightPos, col);

        // col = lerp(AmbientOcclusion.rgb, col, ComputeAO(p, n, AODistance, 3, AmbientOcclusion.a));

        // // We've gone through all steps, but we haven't hit anything.
        // // Mix in the background color.
        // if (D > MinDistance)
        // {
        //     a = 1 - clamp(log(D / MinDistance) * DistToColor, 0.0, 1.0);
        //     col = lerp(col, Background.rgb, a);
        // }
    }
    else
    {
        a = 0;
    }

    // ----------------

    // Glow is based on the number of steps.
    // col = lerp(col, Glow.rgb, float(steps) / float(MaxSteps) * Glow.a);

    // Discard transparent fragments...
    float f = clamp(log(length(p - input.worldTViewPos) / Fog), 0, 1);
    col = lerp(col, Background.rgb, f);
    a *= (1 - f * Background.a);

    if (a < 0.6)
    {
        discard;
    }

    // PBR shading -----------------------

    // Sample input textures to get shading model params.

    // TODO: tri-planar mappping
    float2 uv = p.xy;
    float4 albedo = BaseColorMap.Sample(texSampler, uv);

    float4 roughnessMetallicOcclusion = RSMOMap.Sample(texSampler, uv);
    float roughness = saturate(roughnessMetallicOcclusion.x + Roughness);
    float metalness = saturate(roughnessMetallicOcclusion.y + Metal);
    float occlusion = roughnessMetallicOcclusion.z;

    float3 N = normal;
    float3 Lo = -dp;

    // Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));

    // Specular reflection vector.
    float3 Lr = 2.0 * cosLo * N - Lo;

    // Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = lerp(Fdielectric.xxx, albedo.rgb, metalness).rgb;

    // Direct lighting calculation for analytical lights.
    float3 directLighting = 0.0;

    for (uint i = 0; i < ActiveLightCount; ++i)
    {
        if (i < 0)
            continue;

        float3 Li = Lights[i].position - p; //- Lights[i].direction;
        float distance = length(Li);
        float intensity = Lights[i].intensity / (pow(distance / Lights[i].range, Lights[i].decay) + 1);
        float3 Lradiance = Lights[i].color.rgb * intensity; // Lights[i].radiance;

        // Half-vector between Li and Lo.
        float3 Lh = normalize(Li + Lo);

        // Calculate angles between surface normal and various light vectors.
        float cosLi = max(0.0, dot(N, Li));
        float cosLh = max(0.0, dot(N, Lh));

        // Calculate Fresnel term for direct lighting.
        float3 F = fresnelSchlick(F0, max(0.0, dot(Lh, Lo)));

        // Calculate normal distribution for specular BRDF.
        float D = ndfGGX(cosLh, roughness);
        // Calculate geometric attenuation for specular BRDF.
        float G = gaSchlickGGX(cosLi, cosLo, roughness);

        // Diffuse scattering happens due to light being refracted multiple times by a dielectric medium.
        // Metals on the other hand either reflect or absorb energy, so diffuse contribution is always zero.
        // To be energy conserving we must scale diffuse BRDF contribution based on Fresnel factor & metalness.
        float3 kd = lerp(float3(1, 1, 1), float3(0, 0, 0), metalness);
        // return float4(F, 1);

        // Lambert diffuse BRDF.
        // We don't scale by 1/PI for lighting & material units to be more convenient.
        // See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
        float3 diffuseBRDF = kd * albedo.rgb;

        // Cook-Torrance specular microfacet BRDF.
        float3 specularBRDF = ((F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo)) * Specular.rgb;

        // Total contribution for this light.
        directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi;
    }

    // Ambient lighting (IBL).
    float3 ambientLighting = 0;
    {
        // Sample diffuse irradiance at normal direction.
        uint width, height, levels;
        PrefilteredSpecular.GetDimensions(0, width, height, levels);
        float3 irradiance = PrefilteredSpecular.SampleLevel(texSampler, N, 0.6 * levels).rgb;

        // Calculate Fresnel term for ambient lighting.
        // Since we use pre-filtered cubemap(s) and irradiance is coming from many directions
        // use cosLo instead of angle with light's half-vector (cosLh above).
        // See: https://seblagarde.wordpress.com/2011/08/17/hello-world/
        float3 F = fresnelSchlick(F0, cosLo);

        // Get diffuse contribution factor (as with direct lighting).
        float3 kd = lerp(1.0 - F, 0.0, metalness);

        // Irradiance map contains exitant radiance assuming Lambertian BRDF, no need to scale by 1/PI here either.
        float3 diffuseIBL = kd * albedo.rgb * irradiance;

        // Sample pre-filtered specular reflection environment at correct mipmap level.
        float3 specularIrradiance = PrefilteredSpecular.SampleLevel(texSampler, Lr, roughness * levels).rgb;

        // Split-sum approximation factors for Cook-Torrance specular BRDF.
        float2 specularBRDF = BRDFLookup.SampleLevel(clampedSampler, float2(cosLo, roughness), 0).rg;

        // Total specular IBL contribution.
        float3 specularIBL = (F0 * specularBRDF.x + specularBRDF.y) * specularIrradiance;
        ambientLighting = (diffuseIBL + specularIBL) * occlusion;
    }

    // Final fragment color.
    float4 litColor = float4(directLighting + ambientLighting, 1.0) * BaseColor; // TODO Add parameter * Color;

    // TODO: Fog
    // litColor.rgb = lerp(litColor.rgb, FogColor.rgb, pin.fog);
    litColor += float4(EmissiveColorMap.Sample(texSampler, uv).rgb * EmissiveColor.rgb, 0);
    litColor.a *= albedo.a;

    litColor.rgb = lerp(AmbientOcclusion.rgb, litColor.rgb, ComputeAO(p, N, AODistance, 3, AmbientOcclusion.a));

    PSOutput result;
    float depth = dot(eye - p, -dp);
    result.depth = DepthFromWorldSpace2(depth, 0.01, 1000);
    result.color = litColor;
    return result;
}
