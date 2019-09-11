RWTexture2D<float4> outputTexture : register(u0);

cbuffer TimeConstants : register(b0)
{
    float globalTime;
    float time;
    float2 dummy;
}

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    float2 rg = (float2)i.xy / float2(300, 300);
    float b = sin(time)*0.5 + 0.5;
    outputTexture[i.xy] = float4(rg.x, rg.y, b, 1.0);
}
