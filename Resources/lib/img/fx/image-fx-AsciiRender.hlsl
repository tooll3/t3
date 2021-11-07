cbuffer ParamConstants : register(b0)
{
    float4 Fill;
    float4 Background;    
    float2 Offset;
    float2 FontCharSize;
    float ScaleFactor;
    float Bias;
}

cbuffer TimeConstants : register(b1)
{
    float globalTime;
    float time;
    float runTime;
    float beatTime;
}

cbuffer Resolution : register(b2)
{
    float TargetWidth;
    float TargetHeight;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> ImageA : register(t0);
Texture2D<float4> ImageB : register(t1);
Texture2D<float> ImageC : register(t2);

sampler texSampler : register(s0);
sampler texSamplerPoint : register(s1);


#define mod(x,y) (x-y*floor(x/y))

float4 psMain(vsOutput psInput) : SV_TARGET
{    
    float aspectRatio = TargetWidth/TargetHeight;
    float2 p = psInput.texCoord;
    p-= 0.5;
    float2 fontCharWidth = FontCharSize; 
    float2 divisions = float2(TargetWidth / fontCharWidth.x, TargetHeight / fontCharWidth.y) / ScaleFactor;
    float2 fixOffset = Offset * float2(-1,1)  / divisions;
    p+= fixOffset;

    float2 p1 = p;
    float2 gridSize = float2( 1/divisions.x, 1/divisions.y);
    float2 pInCell = mod(p1, gridSize);
    float2 cellTiles = (p1 - pInCell + 0.5) - fixOffset;

    pInCell *= divisions;

    float4 colFromImageA = ImageA.Sample(texSampler, cellTiles);     
    float grayScale = (colFromImageA.r + colFromImageA.g + colFromImageA.b)/3;

    float dBiased = Bias>= 0 
        ? pow( grayScale, Bias+1)
        : 1-pow( clamp(1-grayScale,0,10), -Bias+1);    

    float letter = ImageC.Sample(texSamplerPoint, float2( dBiased ,0));

    float letterIndex = letter * 256;
    float rowIndex = floor(letterIndex / 16);
    float columnIndex = floor(letterIndex % 16);

    float2 letterPos = float2( columnIndex , rowIndex) / 16;
    float4 colorFromFont = ImageB.Sample(texSamplerPoint, pInCell / 16 + letterPos);    

    if(Background.a  < 1) {
        float4 orgColor =  ImageA.Sample(texSampler, psInput.texCoord);
        return lerp(lerp(orgColor, Background, Background.a), Fill, colorFromFont.r);
    }

    return lerp(Background, Fill, colorFromFont.r);
}