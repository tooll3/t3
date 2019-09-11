RWTexture2D<float4> outputTexture : register(u0);

Texture2D<float4> inputTexture : register(t0);

cbuffer TimeConstants : register(b0)
{
    float globalTime;
    float time;
    float2 dummy;
}

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint width, height;
    outputTexture.GetDimensions(width, height);
    float2 rg = (float2)i.xy / float2(width, height);
    float b = sin(time)*0.5 + 0.5;
    float4 calcColor = float4(rg, b, 1);
    float4 inputColor = inputTexture[i.xy*3];
    float4 outputColor = lerp(calcColor, inputColor, 0.5);
    outputTexture[i.xy] = outputColor;
}
