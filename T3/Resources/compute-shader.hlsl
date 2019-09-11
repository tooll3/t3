RWTexture2D<float4> outputTexture;

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    float f = (float2)i.xy / float2(300, 300);
    outputTexture[i.xy] = float4(f, f, f, 1.0);
}