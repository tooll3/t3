cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float Width;
    float Rotation;
    float ShowOriginal;
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

Texture2D<float4> inputTexture : register(t0);
sampler texSampler : register(s0);


float IsBetween( float value, float low, float high) {
    return (value >= low && value <= high) ? 1:0;
}


float4 psMain(vsOutput psInput) : SV_TARGET
{
    uint width, height;
    inputTexture.GetDimensions(width, height);

    float2 uv = psInput.texCoord;
    float4 orgColor =  inputTexture.Sample(texSampler, uv);

    float aspectRation = TargetWidth/TargetHeight;
    float2 p = uv;
    p-= 0.5;
    p.x *=aspectRation;

    float radians = Rotation / 180 *3.141578;
    float2 angle =  float2(sin(radians),cos(radians));
    float distanceFromCenter=  dot(p-Center * float2(1,-1), angle);
    float normalizedDistance= -distanceFromCenter / Width;
    float4 visibleOrgColor = lerp(float4(0,0,0,0), orgColor, ShowOriginal);

    // Bottom Line
    if( IsBetween(normalizedDistance, 1, 1 + 0.01)) {
        return float4(0., 0., 0., 1);
    }

    float2 pOnLine = p;
    pOnLine +=  (- distanceFromCenter)  *  angle;
    pOnLine.x /= aspectRation;
    pOnLine += 0.5;
    float4 colorOnLine = inputTexture.Sample(texSampler, pOnLine);

    // Curves...
    float4 curveColor = float4(0,0,0,0);
    float lineThickness = 0.015 * width/max(width,height);
    // float3 curveShapeRGB = smoothstep(normalizedDistance +lineThickness, normalizedDistance +lineThickness * 1.5 ,colorOnLine.rgb);
    // float curveShapeA = smoothstep(normalizedDistance +lineThickness, normalizedDistance +lineThickness * 1.5 ,colorOnLine.a) * 0.2;
    float4 curveShape = smoothstep(normalizedDistance +lineThickness, normalizedDistance +lineThickness * 1.5 ,colorOnLine.rgba) * float4(1,1,1, 0.2);

    float4 curveLines =smoothstep(normalizedDistance + lineThickness, normalizedDistance,colorOnLine.rgba)
                    *smoothstep(normalizedDistance - lineThickness, normalizedDistance,colorOnLine.rgba) * float4(1,1,1,0.0);
    curveLines.a += length(curveLines.rgb) * 0.3;
    curveLines.rgb+= curveLines.a * 0.2;
    if(normalizedDistance < 0) {
        curveShape = float4(1,1,1,0.2)  - curveShape;
    }
    curveColor.rgba = curveLines + curveShape;

    if(normalizedDistance < 0)
        curveColor.a =0;
    
    // Zebra pattern for highlight clamping
    float3 clamping = (colorOnLine.rgb > 1 || colorOnLine.rgb < 0) ? float3(1,1,1) :float3(0,0,0);
    float2 pixelposition = uv * float2(width,height);
    float pattern = (pixelposition.x  + pixelposition.y + 0.5 + beatTime * 100)  % 8 < 2 ? 1: -1;

    float3 clampedAreaRGB = clamping * curveShape.rgb * ((normalizedDistance > 1 || normalizedDistance <0) ? 1:0);
    float4 clampedArea = float4(clampedAreaRGB, length(clampedAreaRGB) * pattern * 0.2);
    float heighlightExcessiveAlpha = ((normalizedDistance > 1 || normalizedDistance < 0)  && colorOnLine.a > normalizedDistance) ? 1: 0;

    bool isBetweenCurveRange = normalizedDistance >= 0 && normalizedDistance <= 1;

    return  curveColor
            + clampedArea
            + visibleOrgColor * (isBetweenCurveRange ? 0.4 : 1);
}
