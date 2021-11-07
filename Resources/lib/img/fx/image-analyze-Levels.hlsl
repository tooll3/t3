cbuffer ParamConstants : register(b0)
{
    float2 Center;
    float Width;
    float Rotation;
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
    float4 orgColor =  inputTexture.Sample(texSampler, uv);//clamp(inputTexture.Sample(texSampler, uv), 0, float4(100,100,100,4));

    float aspectRation = TargetWidth/TargetHeight;
    float2 p = uv;
    p-= 0.5;
    p.x *=aspectRation;

    float radians = Rotation / 180 *3.141578;
    float2 angle =  float2(sin(radians),cos(radians));
    float distanceFromCenter=  dot(p-Center, angle);
    float normalizedDistance  = -distanceFromCenter / Width;
    if(normalizedDistance < 0) {
        return orgColor;
    }

    if( IsBetween(normalizedDistance, 1, 1.03)) {
        return float4(0., 0., 0., 1);
    }

    float2 pOnLine = p;
    pOnLine +=  (- distanceFromCenter)  *  angle;
    pOnLine.x /= aspectRation;
    pOnLine += 0.5;
    float4 colorOnLine = inputTexture.Sample(texSampler, pOnLine);

    float4 bgColor = inputTexture.Sample(texSampler, uv);

    // Show curves
    float lineThickness = 0.02 * width/max(width,height);
    float4 curveColor = float4(0,0,0,1);
    //curveColor.rgb = (colorOnLine.rgb < normalizedDistance ) ? 0:0.3;
    float3 curveShape = smoothstep(normalizedDistance +lineThickness, normalizedDistance +lineThickness * 1.5 ,colorOnLine.rgb) * 0.3;
    float3 curveLine =smoothstep(normalizedDistance + lineThickness, normalizedDistance,colorOnLine.rgb)
                    *smoothstep(normalizedDistance - lineThickness, normalizedDistance,colorOnLine.rgb) *1.5;
    curveColor.rgb = curveLine + curveShape;

    // Highlight clamping
    float3 clamping = colorOnLine.rgb > 1 ? float3(1,1,1) :float3(0,0,0);
    float2 pixelposition = uv * float2(width,height);
    float pattern = (pixelposition.x  + pixelposition.y + 0.5 + beatTime * 100)  % 8 < 2 ? 1:0;

    float3 clampArea = clamping * curveShape.rgb * (normalizedDistance > 1 ? 3:0);
    bgColor.rgb -= pattern * (clampArea > 0 ? 10 :0);

    curveColor.a =  normalizedDistance > 1 
                        ? ((colorOnLine.a > normalizedDistance  ) ? 0.3: bgColor.a)
                        : ((colorOnLine.a > normalizedDistance ? 0.8:0.4));

    float heighlightExcessiveAlpha = (normalizedDistance > 1  && colorOnLine.a > normalizedDistance) ? 1: 0;
    bgColor.rgb += pattern * heighlightExcessiveAlpha * 10;

    curveColor.rgb += bgColor * ((normalizedDistance <1) ? 0.6 : 1);
    //curveColor.a = 1;
    return curveColor;
}
