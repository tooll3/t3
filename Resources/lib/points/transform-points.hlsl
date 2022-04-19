#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformMatrix;
    float UpdateRotation;
    float ScaleW;
    float OffsetW;
    float CoordinateSpace;
    float WIsWeight;
}


StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

static const float PointSpace = 0;
static const float ObjectSpace = 1;
static const float WorldSpace = 2;

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        return;
    }

    float w = SourcePoints[i.x].w;
    float3 pOrg = SourcePoints[i.x].position;
    float3 p = pOrg;

    float4 rotation = SourcePoints[i.x].rotation;
    float4 orgRot = rotation;

    if(CoordinateSpace < 0.5) {
        p.xyz = 0;
        rotation = float4(0,0,0,1);
    }

    float3 pLocal = p;
    p = mul(float4(p,1), TransformMatrix).xyz;

    float4 newRotation = rotation;

    // Transform rotation is kind of tricky. There might be more efficient ways to do this.
    if(UpdateRotation > 0.5) 
    {
        float3 xDir = rotate_vector(float3(1,0,0), rotation);
        float3 rotatedXDir = normalize(mul(float4(xDir,0), TransformMatrix).xyz);

        float3 yDir = rotate_vector(float3(0, 1,0), rotation);
        float3 rotatedYDir = normalize(mul(float4(yDir,0), TransformMatrix).xyz);
        
        float3 crossXY = cross(rotatedXDir, rotatedYDir);
        float3x3 orientationDest= float3x3(
            rotatedXDir, 
            cross(crossXY, rotatedXDir), 
            crossXY );

        newRotation = normalize(q_from_matrix(transpose(orientationDest)));        
        if(CoordinateSpace  < 0.5) {
            newRotation = qmul(orgRot, newRotation);
        }
    }

    if(WIsWeight > 0.5) {
        float3 weightedOffset = (p - pLocal) * w;
        p = pLocal + weightedOffset;

        newRotation = q_slerp(rotation,newRotation, w);
    }

    if(CoordinateSpace < 0.5) {     
        p.xyz = rotate_vector(p.xyz, orgRot).xyz;
        p += pOrg;
    } 

    ResultPoints[i.x].position = p.xyz;



    ResultPoints[i.x].rotation = newRotation;
    ResultPoints[i.x].w = SourcePoints[i.x].w * ScaleW + OffsetW;
}

