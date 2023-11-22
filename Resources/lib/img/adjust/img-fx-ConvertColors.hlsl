//RWTexture2D<float4> outputTexture : register(u0);
Texture2D<float4> InputTexture : register(t0);
sampler texSampler : register(s0);

cbuffer ParamConstants : register(b0)
{    
    float Mode;
}

static const float3x3 fwdA = {1.0, 1.0, 1.0,
                       0.3963377774, -0.1055613458, -0.0894841775,
                       0.2158037573, -0.0638541728, -1.2914855480};
                       
static const float3x3 fwdB= {4.0767245293, -1.2681437731, -0.0041119885,
                       -3.3072168827, 2.6093323231, -0.7034763098,
                       0.2307590544, -0.3411344290,  1.7068625689};

static const float3x3 invB = {0.4121656120, 0.2118591070, 0.0883097947,
                       0.5362752080, 0.6807189584, 0.2818474174,
                       0.0514575653, 0.1074065790, 0.6302613616};
                       
static const float3x3 invA = {0.2104542553, 1.9779984951, 0.0259040371,
                       0.7936177850, -2.4285922050, 0.7827717662,
                       -0.0040720468, 0.4505937099, -0.8086757660};

float3 RgbToOkLab(float3 c) {

    float3 lms = mul(invB, c);
    return mul(invA, (sign(lms) * pow(abs(lms), 0.3333333333333)));    
} 

float3 OklabToRgb(float3 c) {
    float3 lms = mul(fwdA, c);
    return mul( fwdB , (lms * lms * lms));    
}

// inline float3 OkLabToLCh(float3 oklab) {
//     float3 polar = 0;
//     polar.x = oklab.x;
//     polar.y = sqrt(oklab.y * oklab.y + oklab.z * oklab.z);
//     polar.z = atan2(oklab.z, oklab.y);
//     return polar;
// }

// inline float3 LChToOkLab(float3 polar) {
//     float3 oklab = 0;
//     oklab.x = polar.x;
//     oklab.y = polar.y * cos(polar.z);
//     oklab.z = polar.y * sin(polar.z);
//     return oklab;
// }

inline float3 RgbToLCh(float3 col) {
    col = mul(col, invB);
    col= mul((sign(col) * pow(abs(col), 0.3333333333333)), invA);    

    float3 polar = 0;
    polar.x = col.x;
    polar.y = sqrt(col.y * col.y + col.z * col.z);
    polar.z = atan2(col.z, col.y);
    return polar; 
}


inline float3 LChToRgb(float3 polar) {
    float3 col = 0; 
    col.x = polar.x;
    col.y = polar.y * cos(polar.z);
    col.z = polar.y * sin(polar.z);

    float3 lms = mul(col, fwdA);
    return mul( (lms * lms * lms), fwdB);   
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


float4 psMain(vsOutput psInput) : SV_TARGET
{
    float2 uv = psInput.texCoord;
    float4 c = InputTexture.SampleLevel(texSampler, uv, 0.0);

    if(Mode< 0.5) {
        return float4(RgbToOkLab(c.rgb),c.a);
    }

    if(Mode < 1.5) {
        return float4(OklabToRgb(c.rgb), c.a); 
    }

    if(Mode < 2.5) 
    {
        return float4(RgbToLCh(c.rgb), c.a);
        //return float4(OkLabToLCh(RgbToOkLab(c.rgb)), c.a);

    }

    if(Mode < 3.5) 
    {
        return float4(LChToRgb(c.rgb), c.a);
        //return float4(OklabToRgb(LChToOkLab(c)).rgb, c.a);
    }

    return c;

    // float4 col = InputTexture.Sample(texSampler, psInput.texCoord);
    // float r = dot(col, float4(MultiplyR.r, MultiplyG.r, MultiplyB.r, MultiplyA.r)) + Add.r;
    // float g = dot(col, float4(MultiplyR.g, MultiplyG.g, MultiplyB.g, MultiplyA.g)) + Add.g;
    // float b = dot(col, float4(MultiplyR.b, MultiplyG.b, MultiplyB.b, MultiplyA.b)) + Add.b;
    // float a = dot(col, float4(MultiplyR.a, MultiplyG.a, MultiplyB.a, MultiplyA.a)) + Add.a;
    // return float4(clamp(float3(r,g,b),0,10000), clamp(a,0.0001,1));   
}
