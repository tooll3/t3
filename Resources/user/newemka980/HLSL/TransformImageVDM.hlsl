cbuffer ParamConstants : register(b0)
{
    float2 Offset;
    float2 Stretch;

    float Scale;    // 4
    float Rotation; // 5

    float RepeatMode; // 6
}

cbuffer Resolution : register(b1)
{
    float TargetWidth;
    float TargetHeight;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> ImageA : register(t0);
sampler texSampler : register(s0);

float2 rotateVec2(float2 vec, float angle) {
    float sina = sin(angle);
    float cosa = cos(angle);
    return float2(
        cosa * vec.x - sina * vec.y,
        sina * vec.x + cosa * vec.y
    );
}

float4 psMain(vsOutput psInput) : SV_TARGET
{
    float height, width;
    ImageA.GetDimensions(height, width);
    float2 aspect2 = width / height;

    float2 uv = psInput.texCoord;

    float sourceAspectRatio = TargetWidth / TargetHeight;

    float2 divisions = float2(sourceAspectRatio / Stretch.x, 1 / Stretch.y) / Scale;
    float2 p = uv;
    float2 offset = Offset * float2(-1, 1); // Translation on X will match the user's movement 
    p += offset;
    p -= 0.5;

    // Rotate UVs
    float imageRotationRad = (-Rotation - 90) / 180 * 3.141578;

    float sina = sin(-imageRotationRad - 3.141578 / 2);
    float cosa = cos(-imageRotationRad - 3.141578 / 2);

    p.x *= sourceAspectRatio;

    p = float2(
        cosa * p.x - sina * p.y,
        cosa * p.y + sina * p.x
    );

    p.x *= aspect2 / sourceAspectRatio;
    p *= divisions;

    float2 samplePos = p + 0.5;

    // Sample the texture
    float4 imgColorForCel = ImageA.Sample(texSampler, samplePos);

    // Apply rotation to the displacement vector
    float3 displacement = imgColorForCel.rgb *float3(-1,1,1);
    displacement.xy = rotateVec2(displacement.yx, -imageRotationRad);

    return float4(displacement, imgColorForCel.a);
}
