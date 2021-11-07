#include "hash-functions.hlsl"
#include "point.hlsl"

StructuredBuffer<Point> SourcePoints : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0; 



cbuffer Params : register(b0)
{
    float MixOriginal;
    float Reset;
};


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    if(Reset > 0.5) {
        ResultPoints[i.x] = SourcePoints[i.x];
        return;
    }

    uint sourcePointcount, stride;
    SourcePoints.GetDimensions(sourcePointcount, stride);
    
    if(i.x >= sourcePointcount) 
    {
        ResultPoints[i.x].w = sqrt(-1);
        ResultPoints[i.x].position = SourcePoints[0].position;
        return;
    }


    float currentW = ResultPoints[i.x].w;
    float orgW = SourcePoints[i.x].w;

    if(isnan(orgW) || isnan(currentW)) 
    {
        ResultPoints[i.x] = SourcePoints[i.x];
        return;
    }

    ResultPoints[i.x].position = lerp(ResultPoints[i.x].position,  SourcePoints[i.x].position, MixOriginal);
    ResultPoints[i.x].w = lerp( currentW, orgW, MixOriginal );
    ResultPoints[i.x].rotation = q_slerp(ResultPoints[i.x].rotation,  SourcePoints[i.x].rotation, MixOriginal);
}