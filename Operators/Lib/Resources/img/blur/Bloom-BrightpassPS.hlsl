    // BrightPassPS.hlsl
    // Outputs pixel color only if its brightness exceeds a threshold.

    // Constant Buffer for threshold parameter
    cbuffer ThresholdParams : register(b0)
    {
        float Threshold;
        // Add padding if needed for alignment, though likely not for a single float
        float3 _Padding;
    };

    // Input Texture
    Texture2D SourceTexture : register(t0);
    // Sampler State (assuming linear filtering might be desirable)
    SamplerState LinearSampler : register(s0);

    // Input structure from Vertex Shader
    struct PS_INPUT
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
    };

    // Function to calculate perceived luminance (adjust weights if needed)
    float Luminance(float3 color)
    {
        // NTSC luminance weights
        return dot(color, float3(0.299f, 0.587f, 0.114f));
        // Alternatively, simple average: return (color.r + color.g + color.b) / 3.0f;
        // Or max component: return max(color.r, max(color.g, color.b));
    }

    // Pixel Shader Main Function
    float4 psMain(PS_INPUT input) : SV_Target
    {
        float4 color = SourceTexture.Sample(LinearSampler, input.uv);

        // Calculate brightness (luminance)
        float brightness = Luminance(color.rgb);

        // Subtract threshold and saturate (keeps positive values, zeros out negative)
        // This creates a smoother falloff than a hard step function.
        // Adjust the subtraction or use smoothstep for different falloffs.
        float contribution = saturate(brightness - Threshold);

        // Output the original color multiplied by its contribution factor
        // Pixels below threshold will have contribution=0 -> output black
        return float4(color.rgb * contribution, color.a); // Preserve original alpha? Or set to contribution?

        // Alternative: Hard threshold
        // return (brightness > Threshold) ? color : float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    