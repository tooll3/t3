#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"

cbuffer Params : register(b0)
{
    float SmoothDistance;
    float SampleMode;
    float2 SampleRange;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output


static uint sourceCount;
float3 SamplePosAtF(float f, out float weight) 
{
    float sourceF = saturate(f) * (sourceCount -1);
    int index = (int)sourceF;
    float fraction = sourceF - index;    
    index = clamp(index,0, sourceCount -1);
    weight = lerp(SourcePoints[index].w, SourcePoints[index+1].w, fraction );
    return lerp(SourcePoints[index].position, SourcePoints[index+1].position, fraction );
}

float4 SampleRotationAtF(float f) 
{
    float sourceF = saturate(f) * (sourceCount -1);
    int index = (int)sourceF;
    float fraction = sourceF - index;    
    index = clamp(index,0, sourceCount -1);
    return q_slerp(SourcePoints[index].rotation, SourcePoints[index+1].rotation, fraction );
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

    if(f <0 || f > 1) {
        ResultPoints[i.x].w = sqrt(-1);
        return;
    }

    int maxSteps = 5;
    float sampledWeight;
    float sumWeight = 0;
    float3 sumPoint =  SamplePosAtF( f, sampledWeight );
    sumWeight += sampledWeight;

    
    for(int step = 1; step <= maxSteps; step++) 
    {
        float d = step * SmoothDistance / maxSteps / sourceCount;
        
        sumPoint += SamplePosAtF( f - d, sampledWeight);
        sumWeight += sampledWeight;
        sumPoint += SamplePosAtF( f + d, sampledWeight);
        sumWeight += sampledWeight;
    }

    sumPoint /= (maxSteps * 2 + 1);
    sumWeight /=(maxSteps * 2 + 1);

    ResultPoints[i.x].position = sumPoint;
    ResultPoints[i.x].w = sumWeight;

    ResultPoints[i.x].rotation = SampleRotationAtF(f);// float4(0,0,0,1);//  p.rotation; //qmul(rotationFromDisplace , SourcePoints[i.x].rotation);
    //ResultPoints[i.x].w = 1;//SourcePoints[i.x].w;
}

