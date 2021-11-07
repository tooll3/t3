Texture2D<float> inputTexture : register(t0);
RWTexture2D<float> outputTexture : register(u0);

cbuffer ParamConstants : register(b0)
{
    float Near;
    float Far;
    float param3;
    float param4;
}

[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    float n = Near;
    float f = Far;
    float3 depth = inputTexture[i.xy];
    float c = (2.0 * n) / (f + n - depth * (f - n));

    outputTexture[i.xy] = c;
}
