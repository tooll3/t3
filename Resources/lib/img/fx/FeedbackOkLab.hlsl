cbuffer ParamConstants : register(b0)
{
    float Displacement;
    float DisplacementOffset;
    float SampleRadius;
    float AddBlurred;

    float Twist;
    float Zoom;
    float2 Offset;

    float Rotate;
    float ShiftHue;
    float ShiftSaturation;
    float ShiftBrightness;

    float AmplifyEdge;
    float Reset;
    float2 LuminosityRange;
    float2 ChromaRange;
    float RangeClamping;

    float TwirlNoise;
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

Texture2D<float4> Image : register(t0);
Texture2D<float4> DisplaceMap : register(t1);
Texture2D<float4> FractalNoise : register(t2);
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

float3 hsb2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z < 0.5 ?
                     // float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
               c.z * 2 * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y)
                     : lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), lerp(c.y, 0, (c.z * 2 - 1)));
}

float3 rgb2hsb(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(
        abs(q.z + (q.w - q.y) / (6.0 * d + e)),
        d / (q.x + e),
        q.x * 0.5);
}

static float PI = 3.141578;


inline float LuminocityFromRgb(float3 c) {
    return (c.r + c.g + c.b) /3;
}

float4 psMain(vsOutput input) : SV_TARGET
{        

    float width, height;
    Image.GetDimensions(width, height);
    float2 uv= input.texCoord;

    // Transform...
    float aspect2 = width / height;
    float sourceAspectRatio = (float)TargetWidth / TargetHeight;

    uv += Offset;
    uv -= 0.5;

    // Rotate
    float imageRotationRad = (-Rotate - 90) / 180 * 3.141578;
    uv.x *= sourceAspectRatio;

    uv /= Zoom;

    float sina = sin(-imageRotationRad - 3.141578 / 2);
    float cosa = cos(-imageRotationRad - 3.141578 / 2);
    uv = float2(
    cosa * uv.x - sina * uv.y,
    cosa * uv.y + sina * uv.x);


    // Debug meaningful OKLab range
    // L 0.7 .. 0.95
    // C 0.05 .. 0.2
    uv.x /= sourceAspectRatio;
    uv += 0.5;

    float sx = SampleRadius / width * sourceAspectRatio;
    float sy = SampleRadius / height;
    float padding =1;


    // float3 cx1 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x + sx, uv.y)).rgb);
    // float3 cx2 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x - sx, uv.y)).rgb);
    // float3 cc = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x, uv.y)).rgb);
    // float3 cy1 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x, uv.y + sy)).rgb);
    // float3 cy2 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x, uv.y - sy)).rgb);

    float3 cx1 = DisplaceMap.Sample(texSampler, float2(uv.x + sx, uv.y));
    float3 cx2 = DisplaceMap.Sample(texSampler, float2(uv.x - sx, uv.y));
    float3 cc = DisplaceMap.Sample(texSampler, float2(uv.x - sx, uv.y));    
    float3 cy1 = DisplaceMap.Sample(texSampler, float2(uv.x, uv.y + sy));
    float3 cy2 = DisplaceMap.Sample(texSampler, float2(uv.x, uv.y - sy));
    

    float2 d = float2(   
        LuminocityFromRgb(cx1.rgb) - LuminocityFromRgb(cx2.rgb), 
        LuminocityFromRgb(cy1.rgb) - LuminocityFromRgb(cy2.rgb));

    d.x /= sourceAspectRatio;

    float a = (d.x == 0 && d.y == 0) ? 0 : atan2(d.x, d.y) + Twist / 180 * 3.14158;

    float angleDelta = FractalNoise.Sample(texSampler, input.texCoord);
    a-= (angleDelta - 0.5) * TwirlNoise;

    float2 direction = float2(sin(a), cos(a));
    float len = length(d);


    float2 delta = len < 0.0001 ? 0: (direction * (-Displacement * len * 10 + DisplacementOffset) / float2(aspect2, 1));

    uv += delta;

    float4 c= Image.Sample(texSampler, uv);

    // LCH flow --------------
    // float3 lch = RgbToLCh(c.rgb);
    // lch.x += ShiftBrightness;
    // lch.y += ShiftSaturation;
    // lch.z += ShiftHue;
    // lch.x += d * AmplifyEdge;

    // // Limit Range    
    // float3 lchWindowCenter = lch - float3(LuminosityRange.y, ChromaRange.y,0.5) + 0.5;
    // float3 s = sign(lchWindowCenter-0.5);

    // float3 window= max(0, abs(lchWindowCenter-.5) - float3(LuminosityRange.x,ChromaRange.x,2) * 0.5) ;

    // float3 windowLimiter = smoothstep(0,1, min(1,window)) * s;
    // lch-= windowLimiter * RangeClamping ;
    
    // lch.x = clamp(lch.x,0,100);
    // lch.y = clamp(lch.y,0,1);
    // c.rgb = LChToRgb(lch);

    // LCH flow 2 -------------------------------
    // float3 lch = RgbToLCh(c.rgb);
    // lch.x += ShiftBrightness;
    // lch.y += ShiftSaturation;
    // lch.z += ShiftHue;
    // lch.x += d * AmplifyEdge;

    // lch.x = clamp(lch.x,0,100);
    // lch.y = clamp(lch.y,0,1);
    // c.rgb = LChToRgb(lch);
    // c.rgb += len.xxx * AmplifyEdge;

    // Limit Range RGB channels -------------
    // This leads to washed out colors and gray scale images due to clamping of light areas
    // float3 lchWindowCenter = c.rgb - LuminosityRange.yyy + 0.5;
    // float3 s = sign(lchWindowCenter-0.5);

    // float3 window= max(0, abs(lchWindowCenter-.5) - LuminosityRange.xxx * 0.5) ;

    // float3 windowLimiter = smoothstep(0,1, min(1,window)) * s;
    // c.rgb-= windowLimiter * RangeClamping ;

    // HSB flow -------------------------------
    float3 av=  rgb2hsb((cx1+cx2+cy1+cy2+cc) / 5);
    

    //c.rgb = clamp( c.rgb + av.rgb * AddBlurred,0, 1000);
    

    float3 lch = rgb2hsb(c.rgb).zyx;
    float3 lchWindowCenter = lch - float3(LuminosityRange.y, ChromaRange.y,0.5) + 0.5;
    float3 s = sign(lchWindowCenter-0.5);
    lch.x += len * AmplifyEdge + ShiftBrightness + av.z * AddBlurred;
    lch.y += ShiftSaturation;
    lch.z += ShiftHue;
    
    float3 window= max(0, abs(lchWindowCenter-.5) - float3(LuminosityRange.x,ChromaRange.x,2) * 0.5) ;

    float3 windowLimiter = smoothstep(0,1, min(1,window)) * s;
    lch-= windowLimiter * RangeClamping ;
    
    c.rgb = hsb2rgb(lch.zyx);
    return c;
    
}