#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float InsertCount;
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
    weight = lerp(SourcePoints[index].W, SourcePoints[index+1].W, fraction );
    return lerp(SourcePoints[index].Position, SourcePoints[index+1].Position, fraction );
}

float4 SampleRotationAtF(float f) 
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

    int subdiv = (int)(InsertCount + 1);

    int segmentIndex = i.x / (subdiv);
    int segmentPointIndex = (i.x % (subdiv));

    float f = (float)segmentPointIndex / subdiv;

    if(f <= 0.001)  {
        ResultPoints[i.x] = SourcePoints[segmentIndex];

    }
    else {
        ResultPoints[i.x].Position = lerp( SourcePoints[segmentIndex].Position,  SourcePoints[segmentIndex + 1].Position, f);
        ResultPoints[i.x].W = lerp( SourcePoints[segmentIndex].W,  SourcePoints[segmentIndex + 1].W, f);
        ResultPoints[i.x].Rotation = qSlerp( SourcePoints[segmentIndex].Rotation,  SourcePoints[segmentIndex + 1].Rotation, f);

        ResultPoints[i.x].Color = lerp( SourcePoints[segmentIndex].Color,  SourcePoints[segmentIndex + 1].Color, f);
        ResultPoints[i.x].Selected = lerp( SourcePoints[segmentIndex].Selected,  SourcePoints[segmentIndex + 1].Selected, f);
        ResultPoints[i.x].Stretch = lerp( SourcePoints[segmentIndex].Stretch,  SourcePoints[segmentIndex + 1].Stretch, f);
    }
    

}

