#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float SmoothDistance;
    float SampleMode;
    float2 SampleRange;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output


static uint sourceCount;
static float3 sumPos =0;
static float sumWeight=0;
static float4 sumColor=0;
static float3 sumStretch=0;
static float3 sumSelected=0;
static int sampledCount=0;

void SamplePosAtF(float f) 
{
    float sourceF = saturate(f) * (sourceCount -1);
    uint index = (int)sourceF;
    if(index > sourceCount -2)
        return;

    float w1= SourcePoints[index].W;
    if(isnan(w1)) {
        return;
    }

    float w2= SourcePoints[index+1].W; 
    if(isnan(w2)) {
        return;
    }

    float fraction = sourceF - index;    
    sumWeight += lerp(w1, w2, fraction );
    sumPos += lerp(SourcePoints[index].Position, SourcePoints[index+1].Position , fraction );
    sumColor += lerp(SourcePoints[index].Color, SourcePoints[index+1].Color , fraction );
    sumStretch += lerp(SourcePoints[index].Stretch, SourcePoints[index+1].Stretch , fraction );
    sumSelected += lerp(SourcePoints[index].Selected, SourcePoints[index+1].Selected , fraction );
    sampledCount++;
}

inline float4 SampleRotationAtF(float f) 
{
    float sourceF = saturate(f) * (sourceCount -1);
    int index = (int)sourceF;
    float fraction = sourceF - index;    
    index = clamp(index,0, sourceCount -1);
    return qSlerp(SourcePoints[index].Rotation, SourcePoints[index+1].Rotation, fraction );
}




[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    ResultPoints.GetDimensions(pointCount, stride);

    if(i.x >= pointCount) {
        return;
    }

    uint stride2;
    SourcePoints.GetDimensions(sourceCount, stride);

    float fNormlized = (float)i.x/pointCount;
    
    float rightFactor = SampleMode > 0.5 ? SampleRange.x : 0;
    float f = SampleRange.x + fNormlized * (SampleRange.y - rightFactor);

    if(f <0 || f >= 1) {
        ResultPoints[i.x].W = sqrt(-1);
        return;
    }

    int maxSteps = 5;
    sumWeight = 0;
    sampledCount = 0;
    SamplePosAtF( f);
    
    for(int step = 1; step <= maxSteps; step++) 
    {
        float d = step * SmoothDistance / maxSteps / sourceCount;
        SamplePosAtF( f - d);
        SamplePosAtF( f + d);
    }

    //sumPos /= (sampledCount);
    
    //sumWeight /=(maxSteps * 2 + 1);
    //sumWeight /= sampledCount;
    
    if(sampledCount==0)
       sumWeight = sqrt(-1);

    ResultPoints[i.x].Position = sumPos / sampledCount;
    ResultPoints[i.x].W = sumWeight / sampledCount;
    ResultPoints[i.x].Rotation = SampleRotationAtF(f); // float4(0,0,0,1);//  p.rotation; //qMul(rotationFromDisplace , SourcePoints[i.x].rotation);
    ResultPoints[i.x].Color = sumColor / sampledCount;
    ResultPoints[i.x].Stretch = sumStretch / sampledCount;
    ResultPoints[i.x].Selected = sumSelected / sampledCount;
    //ResultPoints[i.x].w = 1;//SourcePoints[i.x].w;
}

