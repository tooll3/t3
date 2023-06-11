#include "lib/shared/point.hlsl"
 
RWStructuredBuffer<Point> points :register(u0);

cbuffer Params : register(b0)
{
    float Threshold;
    float Dispersion;
    float CellSize;
    float ClampAccelleration;
    float Time;
}

#include "lib/points/spatial-hash-map/spatial-hash-map-lookup.hlsl"


[numthreads( 256, 1, 1 )]
void DispersePoints(uint3 DTid : SV_DispatchThreadID, uint GI: SV_GroupIndex)
{
    uint pointCount, stride;
    points.GetDimensions(pointCount, stride);
        
    if(DTid.x >= pointCount)
        return; // out of bounds

    float3 position = points[DTid.x].position;
    float3 jitter = (hash33u( uint3(DTid.x, DTid.x + 134775813U, DTid.x + 1664525U) + position * 1000 + Time % 123 ) -0.5f)  * CellSize * 2;
    position+= jitter;

    uint startIndex, endIndex;
    if(GridFind(position, startIndex, endIndex)) 
    {
        const uint particleCount = endIndex - startIndex;
        int count =0;
        float3 sumPosition = 0;

        endIndex = max(startIndex + 256 , endIndex);

        for(uint i=startIndex; i < endIndex; ++i) 
        {
            uint pointIndex = CellPointIndices[i];
            if( pointIndex == DTid.x)
                continue;

            float3 otherPos = points[pointIndex].position;
            float3 direction = position - otherPos;
            float distance = length(direction);
            if(distance <= 0.0001) {
                distance = 0.001;
            }

            if(distance > Threshold)
                continue;
            
            float fallOff = max(pow(((distance)/Threshold), 0.5), 0.0001);
            direction *= fallOff;
            float l = length(direction);
            direction /=l;
            direction *= min(l, ClampAccelleration);

            sumPosition += direction;
            count++;
        }

        if(count > 0) {

            sumPosition /= count;
            points[DTid.x].position += sumPosition * Dispersion;
        }
    }
}