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
    float3 separator = 0;

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
        
        // Set the points to draw the bounding box
        // Back face
        ResultPoints[1].Position = minBounds;
        ResultPoints[2].Position = float3(minBounds.x, maxBounds.y, minBounds.z);
        ResultPoints[3].Position = float3(maxBounds.x, maxBounds.y, minBounds.z);
        ResultPoints[4].Position = float3(maxBounds.x, minBounds.y, minBounds.z);
        ResultPoints[5].Position = minBounds;

        // Front face
        ResultPoints[6].Position = float3(minBounds.x, minBounds.y, maxBounds.z);//identical
        ResultPoints[7].Position = float3(minBounds.x, maxBounds.y, maxBounds.z);
        ResultPoints[8].Position = maxBounds;
        ResultPoints[9].Position = float3(maxBounds.x, minBounds.y, maxBounds.z);
        ResultPoints[10].Position = float3(minBounds.x, minBounds.y, maxBounds.z);//identical

        //Connections between Back and Front
        ResultPoints[11].Position = float3(minBounds.x, maxBounds.y, maxBounds.z);//same as [7]
        ResultPoints[12].Position = float3(minBounds.x, maxBounds.y, minBounds.z);//same as [2]
        ResultPoints[13].Position = separator;

        ResultPoints[14].Position = maxBounds;
        ResultPoints[15].Position = float3(maxBounds.x, maxBounds.y, minBounds.z);//same as [3]
        ResultPoints[16].Position = separator;

        ResultPoints[17].Position = float3(maxBounds.x, minBounds.y, maxBounds.z);//same as [9]
        ResultPoints[18].Position = float3(maxBounds.x, minBounds.y, minBounds.z);//same as [4]
        ResultPoints[19].Position = separator;


        float3 middlePoint = (maxBounds + minBounds) * 0.5;
        ResultPoints[0].Position = middlePoint;
        
    }

    // color helpers for development
    /* ResultPoints[1,5].Color = float4(1,0,0,1); //minBounds
    ResultPoints[7].Color = float4(1,0,1,1);
    ResultPoints[8,14].Color = float4(0,1,0,1);//maxBounds
    ResultPoints[0].Color = float4(0,0,1,1);//middlePoint */

    ResultPoints[i.x].W = 1;
    ResultPoints[i.x].Color = 1;
    ResultPoints[i.x].Stretch = 1;
    ResultPoints[i.x].Selected = 1;
    ResultPoints[i.x].Rotation = float4(0, 0, 0, 1);

    //Separators
    ResultPoints[0].W = NAN;
    ResultPoints[13].W = NAN;
    ResultPoints[16].W = NAN;
    ResultPoints[19].W = NAN;
}
