cbuffer ParamConstants : register(b0)
{
    float Displacement;
    float DisplacementOffset;
    float SampleRadius;
    float Shade;

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

    // if(input.texCoord.y * 10 % 1 < 0.1) {
    //     float u = input.texCoord.x;
    //     float3 t = LChToRgb(float3(0.95, 0.1, (u-0.5)*3.141578*2));
    //     //return float4( RgbToLCh(t),1);
    //     return float4( t,1);
    // }

    uv.x /= sourceAspectRatio;
    uv += 0.5;

    float sx = SampleRadius / width * sourceAspectRatio;
    float sy = SampleRadius / height;
    float padding =1;
    float3 cx1 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x + sx, uv.y)).rgb);
    float3 cx2 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x - sx, uv.y)).rgb);
    float3 cc = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x, uv.y)).rgb);
    float3 cy1 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x, uv.y + sy)).rgb);
    float3 cy2 = RgbToLCh(DisplaceMap.Sample(texSampler, float2(uv.x, uv.y - sy)).rgb);

    //return float4( LChToRgb(cc),1);

    
    float3 lchImpact= float3(0.1,0.4,0);

    float x1 = cx1.x * lchImpact.x + cx1.y * lchImpact.y;
    float x2 = cx2.x * lchImpact.x + cx2.y * lchImpact.y;
    float y1 = cy1.x * lchImpact.x + cy1.y * lchImpact.y;
    float y2 = cy2.x * lchImpact.x + cy2.y * lchImpact.y;

    
    float2 d = float2((x1 - x2), (y1 - y2));
    d.x /= sourceAspectRatio;

    //return float4(abs( d.xy) * 100,0,1);



    //return float4(x1 - x2,0,0,1);

    float a = (d.x == 0 && d.y == 0) ? 0 : atan2(d.x, d.y) + Twist / 180 * 3.14158;

    //a+= input.texCoord.x * 4;

    float2 direction = float2(sin(a), cos(a));
    float len = length(d);


    float2 delta = len < 0.00005 ? 0: (direction * (-Displacement * len * 10 + DisplacementOffset) / float2(aspect2, 1));
    //return float4( abs( delta.xy)* 1000 ,0,1);

    uv += delta;

    float4 c= Image.Sample(texSampler, uv);

    float3 lch = RgbToLCh(c.rgb);

    lch.x += ShiftBrightness;
    lch.y += ShiftSaturation;
    lch.z += ShiftHue;

    //lch.x -=cc.x * 0.0001;

    float3 edge = abs(cx1-cx2) + abs(cy1-cy2);
    lch.x += ((edge.x * 0.1) + (edge.y * 0.01) + (edge.z * 0.00)) * AmplifyEdge;


    // Limit Range
    
    float3 lchWindowCenter = lch - float3(LuminosityRange.y, ChromaRange.y,0.5) + 0.5;
    float3 s = sign(lchWindowCenter-0.5);

    float3 window= max(0, abs(lchWindowCenter-.5) - float3(LuminosityRange.x,ChromaRange.x,2) * 0.5) ;

    float3 windowLimiter = smoothstep(0,1, min(1,window)) * s * RangeClamping;
    lch-= windowLimiter;
    lch.x = clamp(lch.x,0,100);
    lch.y = clamp(lch.y,0,1);

    c.rgb = LChToRgb(lch);
    return c;
    
}