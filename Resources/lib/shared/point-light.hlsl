struct PointLight
{
    float3 position;
    float intensity;

    float4 color; // 4

    float3 spotLightDirection; // 8
    float range;

    float decay; // 12
    float spotLightFov;
    float spotLightEdge;
    int ShadowMode;
};
