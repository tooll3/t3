#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{    
    float ConvertFrom;
    float ConvertTo;
    
    float Amount;
    float Offset;
    
    float Mode;
}

StructuredBuffer<Point> SourcePoints : t0;        

RWStructuredBuffer<Point> ResultPoints : u0;


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint index = (uint)i.x;
    uint pointCount, stride;
    SourcePoints.GetDimensions(pointCount, stride);
    if(index >= pointCount) {        
        return;
    }

    Point p = SourcePoints[index];
    float conversionValue;
    
    float pointAttributes[14];
    
    // loading all point attributes into an array makes it easier to modify them
    pointAttributes[0] = p.Position.x;
    pointAttributes[1] = p.Position.y;
    pointAttributes[2] = p.Position.z;
    pointAttributes[3] = p.Rotation.x;
    pointAttributes[4] = p.Rotation.y;
    pointAttributes[5] = p.Rotation.z;
    pointAttributes[6] = p.Stretch.x;
    pointAttributes[7] = p.Stretch.y;
    pointAttributes[8] = p.Stretch.z;
    pointAttributes[9] = p.Color.r;
    pointAttributes[10] = p.Color.g;
    pointAttributes[11] = p.Color.b;
    pointAttributes[12] = p.Color.a;
    pointAttributes[13] = p.W;
    
    conversionValue = pointAttributes[ConvertFrom];
    
    conversionValue *= Amount; //this works, but most other point attribute ops use lerp
    conversionValue += Offset;
    
    switch (Mode)
    {
        case 0: //replace
            pointAttributes[ConvertTo] = conversionValue;
            break;
        case 1: //add
            pointAttributes[ConvertTo] += conversionValue;
            break;
        case 2: //multiply
            pointAttributes[ConvertTo] *= conversionValue;
            break;
    }
    
    p.Position.x = pointAttributes[0];
    p.Position.y = pointAttributes[1];
    p.Position.z = pointAttributes[2];
    p.Rotation.x = pointAttributes[3];
    p.Rotation.y = pointAttributes[4];
    p.Rotation.z = pointAttributes[5];
    p.Stretch.x = pointAttributes[6];
    p.Stretch.y = pointAttributes[7];
    p.Stretch.z = pointAttributes[8];
    p.Color.r = pointAttributes[9];
    p.Color.g = pointAttributes[10];
    p.Color.b = pointAttributes[11];
    p.Color.a = pointAttributes[12];
    p.W = pointAttributes[13];
    

    //switch (ConvertFrom) 
    //{ 
    //    case 0:
    //        conversionValue = p.Position.x;
    //        break;
    //    case 1:
    //        conversionValue = p.Position.y;
    //        break;
    //    case 2:
    //        conversionValue = p.Position.z;
    //        break;
    //    case 3:
    //        conversionValue = p.Rotation.x;
    //        break;
    //    case 4:
    //        conversionValue = p.Rotation.y;
    //        break;
    //    case 5:
    //        conversionValue = p.Rotation.z;
    //        break;
    //    case 6:
    //        conversionValue = p.Stretch.x;
    //        break;
    //    case 7:
    //        conversionValue = p.Stretch.y;
    //        break;
    //    case 8:
    //        conversionValue = p.Stretch.z;
    //        break;
    //    case 9:
    //        conversionValue = p.Color.r;
    //        break;
    //    case 10:
    //        conversionValue = p.Color.g;
    //        break;
    //    case 11:
    //        conversionValue = p.Color.b;
    //        break;
    //    case 12:
    //        conversionValue = p.Color.a;
    //        break;
    //    case 13:
    //        conversionValue = p.W;
    //        break;
    //}
    
    //conversionValue *= Amount;
    //conversionValue += Offset;
    
    //switch (ConvertTo)
    //{
    //    case 0:
    //        p.Position.x = conversionValue;
    //        break;
    //    case 1:
    //        p.Position.y = conversionValue;
    //        break;
    //    case 2:
    //        p.Position.z = conversionValue;
    //        break;
    //    case 3:
    //        p.Rotation.x = conversionValue;
    //        break;
    //    case 4:
    //        p.Rotation.y = conversionValue;
    //        break;
    //    case 5:
    //        p.Rotation.z = conversionValue;
    //        break;
    //    case 6:
    //        p.Stretch.x = conversionValue;
    //        break;
    //    case 7:
    //        p.Stretch.y = conversionValue;
    //        break;
    //    case 8:
    //        p.Stretch.z = conversionValue;
    //        break;
    //    case 9:
    //        p.Color.r = conversionValue;
    //        break;
    //    case 10:
    //        p.Color.g = conversionValue;
    //        break;
    //    case 11:
    //        p.Color.b = conversionValue;
    //        break;
    //    case 12:
    //        p.Color.a = conversionValue;
    //        break;
    //    case 13:
    //        p.W = conversionValue;
    //        break;
    //}

    ResultPoints[index] = p; 
}

