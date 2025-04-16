// UpsampleAddPS.hlsl
// Samples a lower-resolution texture using full-resolution UVs (bilinear upsample),
// applies intensity, and outputs for additive blending.

// Constant Buffer
cbuffer CompositeParams : register(b0)
{
    float2 InvTargetSize; // 1/TargetWidth, 1/TargetHeight (of the composite target)
    float2 InvSourceSize; // 1/SourceWidth, 1/SourceHeight (of the low-res texture being sampled)
    float PassIntensity;   // Combined overall Intensity * normalized level weight
};

// Input Texture (Lower resolution blurred texture)
Texture2D LowResTexture : register(t0);
// Sampler State (Linear required for good upsampling)
SamplerState LinearSampler : register(s0);

// Input structure from Vertex Shader
struct PS_INPUT
{
    float4 pos : SV_POSITION;
    float2 uv  : TEXCOORD0;   // UV coordinates for the full-resolution composite target
};

// Pixel Shader Main Function
float4 psMain(PS_INPUT input) : SV_Target
{
    // Sample the low-resolution texture using the full-resolution UVs.
    // The linear sampler handles the interpolation (bilinear upsampling).
    float4 color = LowResTexture.SampleLevel(LinearSampler, input.uv, 0); // Use mip level 0


    // Apply the combined intensity for this pass
    // The additive blending is handled by the Output Merger blend state set in C#
    return color * PassIntensity;
}
