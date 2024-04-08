#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    
}

cbuffer Params : register(b1)
{
    int SourceCount;
    int ResultCount;
   


}

StructuredBuffer<Point> SourcePoints : t0;   // input
RWStructuredBuffer<Point> ResultPoints : u0; // output


[numthreads(64, 1, 1)] void main(uint3 i : SV_DispatchThreadID)
{
    uint index = (uint)i.x;
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    if(index >= pointCount) {        
        return;
    }
   
    if (i.x >= ResultCount)
        return;

    // Retrieve source point
    Point sourcePoint = SourcePoints[index];

    // Check if W value is -nan(ind), if so, skip this point
    if (isnan(sourcePoint.W)) {
        return;
    }

    // Initialize min and max bounds with the first point
    float3 minBounds = SourcePoints[0].Position;
    float3 maxBounds = SourcePoints[0].Position;

 // Iterate over all valid source points to find the min and max bounds
    for (uint j = index + 1; j < pointCount; ++j)
    {
        Point nextPoint = SourcePoints[j];
        if (!isnan(nextPoint.W)) {
            minBounds = min(minBounds, nextPoint.Position);
            maxBounds = max(maxBounds, nextPoint.Position);
        }
    }

    // Set the bounding box points in the ResultPoints buffer
    if (i.x == 0)
    {
        
    

        // Set the eight points of the bounding box
        ResultPoints[0].Position = minBounds;
        ResultPoints[1].Position = float3(maxBounds.x, minBounds.y, minBounds.z);
        ResultPoints[2].Position = float3(minBounds.x, maxBounds.y, minBounds.z);
        ResultPoints[3].Position = float3(maxBounds.x, maxBounds.y, minBounds.z);
        ResultPoints[4].Position = float3(minBounds.x, minBounds.y, maxBounds.z);
        ResultPoints[5].Position = float3(maxBounds.x, minBounds.y, maxBounds.z);
        ResultPoints[6].Position = float3(minBounds.x, maxBounds.y, maxBounds.z);
        ResultPoints[7].Position = maxBounds;

        float3 middlePoint = (maxBounds + minBounds) * 0.5;
        ResultPoints[8].Position = middlePoint;
        
        // Set other properties of bounding box points if needed
        for (uint k = 0; k < 9; ++k)
        {
            ResultPoints[k].W = 1;
            ResultPoints[k].Color = 1;
            ResultPoints[k].Stretch = 1;
            ResultPoints[k].Selected = 1;
            ResultPoints[k].Rotation = float4(0, 0, 0, 1);
        }
    }

    ResultPoints[0].Color = float4(1,0,0,1);
    ResultPoints[7].Color = float4(0,1,0,1);

  
    ResultPoints[8].Color = float4(0,0,1,1);
}
