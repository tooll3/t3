cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float A;
    float B;
    float C;
    float D;

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

Texture2D<float4> X_negative : register(t0);
Texture2D<float4> X_positive : register(t1);
Texture2D<float4> Z_positive : register(t2);
Texture2D<float4> Z_negative : register(t3);
Texture2D<float4> Y_positive : register(t4);
Texture2D<float4> Y_negative : register(t5);
sampler Sampler : register(s0);

float4 psMain(vsOutput input) : SV_TARGET
{
    float width, height;
    X_negative.GetDimensions(width, height);
    float4 c = float4(1,1,0,1);
    float2 uv = input.texCoord;

    // from Bartosz
    // https://stackoverflow.com/questions/34250742/converting-a-cubemap-into-equirectangular-panorama

    float phi = input.texCoord.x * 3.1415 * 2;
    float theta = input.texCoord.y * 3.1415;

    float x = sin(phi) * sin(theta) * -1;
    float y = cos(theta);
    float z = cos(phi) * sin(theta) * -1;

    float a = max(abs(x), max(abs(y), abs(z)));
    float xa = x / a;
    float ya = y / a;
    float za = z / a;

    float espilon = 0.0001;
    #define equal(a,b) (abs(a-b)<espilon)

    c.rgb = float3(x,y,z);

    if (equal(xa, 1.))
    {
      uv = (float2(-za, -ya)+1)/2;
      c.rgb = Z_negative.Sample(Sampler, uv);
    }
    else if (equal(xa, -1.))
    {
      uv = (float2(za, -ya)+1)/2;
      c.rgb = Z_positive.Sample(Sampler, uv);
    }
    else if (equal(za, 1.))
    {
      uv = (float2(xa, -ya)+1)/2;
      c.rgb = X_negative.Sample(Sampler, uv);
    }
    else if (equal(za, -1.))
    {
      uv = (float2(-xa, -ya)+1)/2;
      c.rgb = X_positive.Sample(Sampler, uv);
    }
    else if (equal(ya, 1.))
    {
      uv = (float2(-xa, -za)+1)/2;
      c.rgb = Y_positive.Sample(Sampler, uv);
    }
    else // if (equal(ya, -1.))
    {
      uv = (float2(xa, -za)+1)/2;
      c.rgb = Y_negative.Sample(Sampler, uv);
    }
    
    return c;
}