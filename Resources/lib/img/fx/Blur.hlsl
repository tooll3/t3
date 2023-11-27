cbuffer ParamConstants : register(b0)
{
    float2 Direction;
    float Size;
    float NumberOfSamples;
    float widthToHeight;

    float Offset;
    float Glow2;

    // float Angle;
    // float AngleOffset;
    // float Steps;
    // float Fade;
}

cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> InputTexture : register(t0);
sampler samLinear : register(s0);


static const int WEIGHT_COUNT = 10;
static const float Gauss[WEIGHT_COUNT] = { 0.93, 0.86, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1 };


float4 psMain(vsOutput input) : SV_TARGET
{
	// float2 curPos = input.texCoord;
    // curPos += float2(CenterX, CenterY);
	
    // float4 c = InputTexture.Sample(texSampler, curPos);    
    // c.ra=0.7;
    // return c;

    //return float4(NumberOfSamples,0,0,1);

    float2 dir = Direction;
    dir *= 0.01*Size/NumberOfSamples;
    dir.y *= widthToHeight;

    float2 pos = dir;
    float4 c = InputTexture.Sample(samLinear, input.texCoord);
    float totalWeight = 1;
    for (int i = 0; i < NumberOfSamples; ++i)
    {
        float index = (float)i*(WEIGHT_COUNT - 1)/NumberOfSamples;
        float weight = lerp(Gauss[(int)index], Gauss[(int)index + 1], frac(index));
        c += InputTexture.Sample(samLinear, input.texCoord + pos)*weight;
        c += InputTexture.Sample(samLinear, input.texCoord - pos)*weight;
        pos += dir;
        totalWeight += 2*weight;
    }

    //c.rgb = float3(Offset, Offset, Offset) + c.rgb/totalWeight*Glow;
    //c.a = 1.0;
    c.rgb = c/totalWeight*Glow2 + Offset;
    c.a = clamp(c.a/totalWeight, 0,1);
    //c.ra=1;
    return  c;//clamp(c,0,1000);
}


