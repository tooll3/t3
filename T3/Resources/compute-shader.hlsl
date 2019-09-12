RWTexture2D<float4> outputTexture : register(u0);

Texture2D<float4> inputTexture : register(t0);
sampler texSampler : register(s0);

cbuffer TimeConstants : register(b0)
{
    float globalTime;
    float time;
    float2 dummy;
}

cbuffer ParamConstants : register(b1)
{
    float param1;
    float param2;
    float param3;
    float param4;
}

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint width, height;
    outputTexture.GetDimensions(width, height);

    float2 uv = (float2)i.xy / float2(width, height);
    float b = sin(time)*0.5 + 0.5;
    float4 calcColor = float4(uv, b, 1);
//    uv = uv*2.0 - 1.0;
//    float l = length(uv);
//    uv *= sin(l*time);
//    uv *= b;//sin(time);
//    uv = uv*0.5 + 0.5;
    float4 inputColor = inputTexture.SampleLevel(texSampler, uv, 0);
    float4 outputColor = lerp(calcColor, inputColor, 0.5);
outputColor.r = param1;

    outputTexture[i.xy] = outputColor;
}
