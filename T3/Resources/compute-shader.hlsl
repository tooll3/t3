RWTexture2D<float4> outputTexture : register(u0);

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    float2 f = (float2)i.xy / float2(300, 300);
    outputTexture[i.xy] = float4(f.x, f.y, 0.0, 1.0);
}