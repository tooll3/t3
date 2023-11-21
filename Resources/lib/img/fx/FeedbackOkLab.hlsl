cbuffer ParamConstants : register(b0)
{
    float LimitDarks;
    float LimitBrights;
    float ShiftBrightness;
    float Hue;
    float Saturation;

    float DetectEdges;

    float SampleRadius;
}

// cbuffer Resolution : register(b1)
// {
//     float TargetWidth;
//     float TargetHeight;
// }

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> Image : register(t0);
Texture2D<float4> DisplaceMap : register(t1);
sampler texSampler : register(s0);

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


// //conversion from linear srgb to oklab colorspace
// inline float3 RgbToOkLab(float3 col) {
//     col = mul(col, lrgb2cone);
//     col = pow(col, 1.0 / 3.0);
//     col = mul(col, cone2lab);
//     return col;
// }

// //conversion from oklab to linear srgb
// inline float3 OklabToRgb(float3 col) {
//     col = mul(col, cone2lrgb);
//     col = col * col * col;
//     col = mul(col, lab2cone);
//     return col;
// }


// //conversion from Oklab colorspace to polar LCh colorspace
// inline float3 OkLabToLCh(float3 oklab) {
//     float3 polar = 0;
//     polar.x = oklab.x;
//     polar.y = sqrt(oklab.y * oklab.y + oklab.z * oklab.z);
//     polar.z = atan2(oklab.z, oklab.y);
//     return polar;
// }

// //conversion from Oklab colorspace to polar LCh colorspace
// inline float3 LChToOkLab(float3 polar) {
//     float3 oklab = 0;
//     oklab.x = polar.x;
//     oklab.y = polar.y * cos(polar.z);
//     oklab.z = polar.y * sin(polar.z);
//     return oklab;
// }


inline float3 RgbToLCh(float3 col) {
    col = mul(invB, col);
    col= mul(invA, (sign(col) * pow(abs(col), 0.3333333333333)));    

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

    float3 lms = mul(fwdA, col);
    return mul( fwdB , (lms * lms * lms));   
}


float4 psMain(vsOutput input) : SV_TARGET
{        
    float2 uv= input.texCoord;

    float width, height;
    Image.GetDimensions(width, height);

    float4 displace = DisplaceMap.Sample(texSampler, uv);
    
    uv+= displace.gb * 0.001;
    float4 c= Image.Sample(texSampler, uv);

    float3 lch = RgbToLCh(c.rgb);
    c.rgb = LChToRgb(lch);

    //c=0.001;
    return c;
    
    // float sx = SampleRadius / width;
    // float sy = SampleRadius / height;

    // float4 y1= Image.Sample(texSampler, float2(uv.x,       uv.y + sy));
    // float4 y2= Image.Sample(texSampler, float2(uv.x,       uv.y - sy));
    
    // float4 x1= Image.Sample(texSampler,  float2(uv.x + sx, uv.y));
    // float4 x2= Image.Sample(texSampler,  float2(uv.x - sx, uv.y)); 

    // float4 xy1= Image.Sample(texSampler, float2(uv.x+sx* 0.7 ,       uv.y + sy* 0.7 ) );
    // float4 xy2= Image.Sample(texSampler, float2(uv.x+sx* 0.7 ,       uv.y - sy* 0.7 ) );
    
    // float4 xy3= Image.Sample(texSampler,  float2(uv.x - sx * 0.7, uv.y + sy * 0.7));
    // float4 xy4= Image.Sample(texSampler,  float2(uv.x - sx * 0.7, uv.y - sy * 0.7)); 
    
    // float4 average =  (c + y1 + y2 + x1 + x2  + (xy1+ xy2 + xy3+ xy4) * 0.75    ) / 8;
    // float averageGray = (average.x + average.y + average.z)/3;

    // // Detect Edges
    // const float increasedEdgeParmeterResolution = 100;
    // float edgeDelta =  (           
    //                 abs(x1.r-c.r) + abs(x2.r-c.r) + abs(y1.r - c.r) +abs(y2.r - c.r) +
    //                 abs(x1.g-c.g) + abs(x2.g-c.g) + abs(y1.g - c.g) +abs(y2.g - c.g) +
    //                 abs(x1.b-c.b) + abs(x2.b-c.b) + abs(y1.b - c.b) +abs(y2.b - c.b)
    //             ) * DetectEdges / increasedEdgeParmeterResolution;

    // // Limit value range    
    // const float lowerRange = 0.1;
    // const float upperRange = 0.8;

    // float lowerD =  pow(clamp(lowerRange - averageGray, 0,10), 2) * LimitDarks;

    // float upperD = -pow(clamp(averageGray - upperRange, 0,10), 2) * LimitBrights;
    // float limitShift = lowerD + upperD;


    // c.rgb += limitShift + ShiftBrightness + edgeDelta;
    
    // // Shift colors
    // float3 hsv = rgbToHsv(c.r, c.g, c.b);
    // hsv += float3(Hue, Saturation, 0);
    // c.rgb = hsvToRgb(hsv.x, hsv.y, hsv.z);

    
    // c.a = clamp(c.a, 0,1);
    // c.rgb = clamp(c.rgb, 0.0001, 1000);
    
    // return c;
}