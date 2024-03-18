#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/hash-functions.hlsl"

cbuffer Params : register(b0)
{
    float3 GridSize;
    float _padding1;

    float3 GridOffset;
    float _padding3;

    float3 RandomizeGrid;
    float _padding4;

    float StrokeLength;
    float Speed;
    float PhaseOffset;
}


static const int3 TransitionSteps[] = 
{
    // Source      
    int3(0, 0, 0), // 0
    int3(0, 0, 1), // 1
    int3(1, 0, 1), // 2
    int3(1, 1, 1), // 3
    int3(1, 1, 2), // 4
    int3(2, 1, 2), // 5
    int3(2, 2, 2), // 6
    int3(2, 2, 3), // 7 
    int3(3, 2, 3), // 8 
    int3(3, 3, 3), // 9
    int3(3, 3, 3), // 10
};

static const int3 AxisOrders[] = 
{
    int3(2, 1, 0), // 0
    int3(0, 2, 1), // 0
    int3(1, 0, 2), // 0
    int3(2, 1, 0), // 0
    int3(2, 0, 1), // 0
};
 
StructuredBuffer<Point> StartPoints : t0;
StructuredBuffer<Point> TargetPoints : t1;
RWStructuredBuffer<Point> ResultPoints : u0;

[numthreads(11,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint totalCount, countA, countB, stride;
    ResultPoints.GetDimensions(totalCount, stride);
    StartPoints.GetDimensions(countA, stride);
    TargetPoints.GetDimensions(countB, stride);

    if(i.x > totalCount)
        return;

    const int stepsPerPairCount = 11;
    if(i.x > (uint)totalCount * stepsPerPairCount)
        return;

    uint lineIndex = i.x / stepsPerPairCount;
    uint lineStepIndex = i.x % stepsPerPairCount;

    Point A = StartPoints[lineIndex % (uint)countA];
    Point B = TargetPoints[lineIndex % (uint)countB];

    float2 hash = hash21(lineIndex);
    int3 axisOrder =  AxisOrders[(int)(hash.x*4)]; // int3(2,1,0);

    float3 randomOffset = (hash31(lineIndex + 321) * 2 -1) * RandomizeGrid;
    float3 posA = (A.Position + 0.0001) / GridSize + fmod(GridOffset , GridSize);
    float3 posB = (B.Position + 0.0001) / GridSize + fmod(GridOffset , GridSize);

    float3 transition[] = {
        posA,
        floor(posA) + (hash.x > 0.5 ? 1 : 0) + randomOffset,
        floor(posB) + (hash.y > 0.5 ? 1 : 0) + randomOffset,
        posB
    };

    float3 previousPos = 0;
    float3 p = 0;
    float d =  0;

    float4 stepPositions[11];

    for(int step =0; step <= 10; step++) 
    {
        int3 factorsForStep = TransitionSteps[step];

        p = float3(
            transition[factorsForStep[axisOrder.x]].x,
            transition[factorsForStep[axisOrder.y]].y,
            transition[factorsForStep[axisOrder.z]].z
        );

        if(step > 0) 
        {
            d += length(p - previousPos);
        }

        stepPositions[step] = float4(p, 
                                     1-A.W * Speed * StrokeLength + d / StrokeLength  + PhaseOffset);        
        previousPos = p;
    }
    
    // ========== INSERT SHARED MEMORY BOUNDARY ===================

    float4 prev = stepPositions[ max(0, lineStepIndex-1)];
    float4 current = stepPositions[ lineStepIndex];
    float4 next = stepPositions[ min(lineStepIndex + 1, 10)];

    float w = 1;
    const float NaN = sqrt(-1); // 0.1f;//

    p = current.xyz;
    d = current.w;
    //float d2 = d;

    // Case A1
    if( current.w < 0 && next.w > 1) {
        float a = abs(current.w);
        float b = next.w;
        float f = saturate(b / (a+b));
        p.xyz = lerp(current.xyz, next.xyz, 1-f);
        d = 0;
    }
    // Case A2
    else if( prev.w < 0 && current.w > 1) {
        float a = abs(current.w) -1 ;
        float b = abs(prev.w) + 1;
        float f = saturate(a / (a+b));
        p.xyz = lerp(prev.xyz, current.xyz, 1-f);
        d = 1;
    }

    // Case B0
    else if(current.w <=0  && next.w < 0) {
        w = NaN;
        //d =0;
    }

    // Case B1
    else if(current.w <= 0 && next.w > 0 && next.w < 1) 
    {
        float a = -current.w;
        float b = next.w;
        float f = saturate(a / (a+b));
        p.xyz = lerp(p, next.xyz, f);
        d =0;
        //w =2;
    }

    // Case B2
    else if(current.w >= 0 && next.w < 1) {
        //p.z += 1.1;
    }

    // Case B3
    else if(prev.w < 1 && current.w > 1) {
        float a = 1 - prev.w;
        float b = current.w - 1;
        float f = saturate(a / (a+b));
        p.xyz = lerp(prev.xyz, p, f);
        d = 1;
    }

    // Case B4
    else if(prev.w > 1 && current.w > 1) {
        w = NaN;
    }


    ResultPoints[i.x].Position = (p - fmod(GridOffset,1)) * GridSize;
    //ResultPoints[i.x].position.z += current.w;

    ResultPoints[i.x].W =  1-d * w;

    if( lineStepIndex == 10)
        ResultPoints[i.x].W = NaN; // NaN for divider
}
