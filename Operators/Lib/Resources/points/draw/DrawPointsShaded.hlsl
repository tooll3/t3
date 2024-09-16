#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/point-light.hlsl"
#include "shared/pbr.hlsl"

static const float3 Corners[] = 
{
  float3(-1, -1, 0),
  float3( 1, -1, 0), 
  float3( 1,  1, 0), 
  float3( 1,  1, 0), 
  float3(-1,  1, 0), 
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
    float4 Color;
    
    float Size;
    float SegmentCount;
    float CutOffTransparent;
    float FadeNearest;
    float UseWForSize;
    float RoundShading;
};

cbuffer FogParams : register(b2)
{
    float4 FogColor;
    float FogDistance;
    float FogBias;   
}

cbuffer PointLights : register(b3)
{
    PointLight Lights[8];
    int ActiveLightCount;
}

cbuffer PbrParams : register(b4)
{
    float4 BaseColor;
    float4 EmissiveColor;
    float Roughness;
    float Specular;
    float Metal;
}

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
    float fog : FOG;
    float3 posInWorld: POSITION2;
    //float3x3 tbnToWorld : TBASIS;    
};

sampler texSampler : register(s0);
sampler clampedSampler : register(s1);


StructuredBuffer<Point> Points : t0;
StructuredBuffer<float4> Colors : t1;

Texture2D<float4> BaseColorMap : register(t1);
Texture2D<float4> EmissiveColorMap : register(t2);
Texture2D<float4> RSMOMap : register(t3);
Texture2D<float4> NormalMap : register(t4);

TextureCube<float4> PrefilteredSpecular: register(t5);
Texture2D<float4> BRDFLookup : register(t6);

psInput vsMain(uint id: SV_VertexID)
{
    psInput output;

    int quadIndex = id % 6;
    int particleId = id / 6;
    Point pointDef = Points[particleId];

    float3 quadPos = Corners[quadIndex];
    output.texCoord = (quadPos.xy * 0.5 + 0.5);

    float4 posInObject = float4(pointDef.Position,1);
    float4 quadPosInCamera = mul(posInObject, ObjectToCamera);

    uint colorCount, stride;
    Colors.GetDimensions(colorCount, stride);
    uint colorIndex = (float)particleId/SegmentCount * colorCount;
    float4 dynaColor = colorCount > 0 ? Colors[colorIndex] : 1;
    output.color = pointDef.Color * Color * dynaColor;

    output.posInWorld = mul(quadPosInCamera, CameraToWorld).xyz;

    // Shrink too close particles
    float4 posInCamera = mul(posInObject, ObjectToCamera);
    float tooCloseFactor =  saturate(-posInCamera.z/FadeNearest -1);
    output.color.a *= tooCloseFactor;

    float sizeFactor = UseWForSize > 0.5 ? pointDef.W : 1;

    quadPosInCamera.xy += quadPos.xy*0.050  * sizeFactor * Size * tooCloseFactor;
    output.position = mul(quadPosInCamera, CameraToClipSpace);
    float4 posInWorld = mul(posInObject, ObjectToWorld);

    // Fog
    output.fog = pow(saturate(-posInCamera.z/FogDistance), FogBias);
    return output;
}

static float3 LightPosition = float3(1,2,0);

float4 psMain(psInput pin) : SV_TARGET
{    
    // Sphere Shading...
    float2 p = pin.texCoord * float2(2.0, 2.0) - float2(1.0, 1.0);
    float d= dot(p, p);
    if (d > 0.93)
         discard;
   
    float z = sqrt(1 - d*d)*1.2;
    float3 normal = normalize(float3(p, z));
    float3 lightDir = normalize(LightPosition - pin.posInWorld.xyz);
    //lightDir = mul(float4(lightDir,1), ObjectToWorld);
    normal = mul(float4(normal,0), CameraToWorld).xyz;

    // Sample input textures to get shading model params.
    //float4 albedo =   BaseColorMap.Sample(texSampler, pin.texCoord);
    float4 albedo = pin.color;
    //return float4(normal,1);
    // if(AlphaCutOff > 0 && albedo.a < AlphaCutOff) {
    //     discard;
    // }

    // float4 roughnessSpecularMetallic = RSMOMap.Sample(texSampler, pin.texCoord);
    // float metalness = roughnessSpecularMetallic.z + Metal;
    // float normalStrength = roughnessSpecularMetallic.y;
    // float roughness = roughnessSpecularMetallic.x + Roughness;
    
    float4 roughnessMetallicOcclusion = RSMOMap.Sample(texSampler, pin.texCoord);
    float roughness = saturate(roughnessMetallicOcclusion.x + Roughness);
    float metalness = saturate(roughnessMetallicOcclusion.y + Metal);
    float occlusion = roughnessMetallicOcclusion.z;

    // Outgoing light direction (vector from world-space fragment position to the "eye").
    float3 eyePosition =  mul( float4(0,0,0,1), CameraToWorld);
    float3 Lo = normalize(eyePosition - pin.posInWorld);

    // Get current fragment's normal and transform to world space.
    //float3 N = lerp(float3(0,0,1),  normalize(2.0 * NormalMap.Sample(texSampler, pin.texCoord).rgb - 1.0), normalStrength);
    float3 N = normal;

    //N = normalize(mul(N,pin.tbnToWorld));
    
    // Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));
        
    // Specular reflection vector.
    float3 Lr = 2.0 * cosLo * N - Lo;

    // Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = lerp(Fdielectric, albedo, metalness);

    // Direct lighting calculation for analytical lights.
    float3 directLighting = 0.0;
    for(uint i=0; i < ActiveLightCount; ++i)
    {
        float3 Li = Lights[i].position - pin.posInWorld; //- Lights[i].direction;
        float distance = length(Li);
        float intensity = Lights[i].intensity / (pow(distance/Lights[i].range, Lights[i].decay) + 1);
        float3 Lradiance = Lights[i].color * intensity; // Lights[i].radiance;

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
        float3 specularBRDF = ((F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo)) * Specular;

        // Total contribution for this light.
        directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi;
    }


    // Ambient lighting (IBL).
    float3 ambientLighting = 0;
    {
        // Sample diffuse irradiance at normal direction.
        // float3 irradiance = 0;// irradianceTexture.Sample(texSampler, N).rgb;
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
		float2 specularBRDF = BRDFLookup.SampleLevel(clampedSampler, float2(cosLo, roughness),0).rg;
        
		// Total specular IBL contribution.
		float3 specularIBL = (F0 * specularBRDF.x + specularBRDF.y) * specularIrradiance;
        ambientLighting = (diffuseIBL + specularIBL) * occlusion;
    }

    // Final fragment color.
    float4 litColor= float4(directLighting + ambientLighting, 1.0) * BaseColor * Color;


    litColor.rgb = lerp(litColor.rgb, FogColor.rgb, pin.fog);
    litColor += float4(EmissiveColorMap.Sample(texSampler, pin.texCoord).rgb * EmissiveColor.rgb, 0);
    litColor.a *= albedo.a;
    return litColor;






    //float4 textureCol = texture2.Sample(texSampler, input.texCoord);    
    // if(textureCol.a < CutOffTransparent)
    //     discard;


    // float diffuse = lerp(1, saturate(dot(normal, lightDir)), RoundShading);

    // return float4(diffuse.xxx,1);

    // float4 col = pin.color;
    // col.rgb = lerp(col.rgb, FogColor.rgb, pin.fog);
    // return clamp(col, float4(0,0,0,0), float4(1000,1000,1000,1));
}
