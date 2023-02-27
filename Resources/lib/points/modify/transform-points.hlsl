#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
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

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if (i.x >= numStructs)
    {
        return;
    }

    float w = SourcePoints[i.x].w;
    float3 pOrg = SourcePoints[i.x].position;
    float3 p = pOrg;

    float4 orgRot = SourcePoints[i.x].rotation;
    float4 rotation = orgRot;

    if (CoordinateSpace < 0.5)
    {
        p.xyz = 0;
        rotation = float4(0, 0, 0, 1);
    }

    float3 pLocal = p;
    p = mul(float4(p, 1), TransformMatrix).xyz;

    float4 newRotation = rotation;

    // Transform rotation is kind of tricky. There might be more efficient ways to do this.
    if (UpdateRotation > 0.5)
    {
        // float3 xDir = rotate_vector(float3(1,0,0), rotation);
        // float3 rotatedXDir = normalize(mul(float4(xDir,0), TransformMatrix).xyz);

        // float3 yDir = rotate_vector(float3(0, 1,0), rotation);
        // float3 rotatedYDir = normalize(mul(float4(yDir,0), TransformMatrix).xyz);

        // float3 crossXY = cross(rotatedXDir, rotatedYDir);
        // float3x3 orientationDest= float3x3(
        //     rotatedXDir,
        //     cross(crossXY, rotatedXDir),
        //     crossXY );

        float3x3 orientationDest = float3x3(
            TransformMatrix._m00_m01_m02,
            TransformMatrix._m10_m11_m12,
            TransformMatrix._m20_m21_m22);

        newRotation = normalize(quaternion_from_matrix_precise(transpose(orientationDest)));

        // Adjust rotation in point space
        if (CoordinateSpace < 0.5)
        {
            newRotation = qmul(orgRot, newRotation);
        }
        else
        {
            newRotation = qmul(newRotation, orgRot);
        }
    }

    float weight = 1;

    if (WIsWeight >= 0.5)
    {
        float3 weightedOffset = (p - pLocal) * w;
        p = pLocal + weightedOffset;
        weight = w;

        // newRotation *= w;
        newRotation = q_slerp(orgRot, newRotation, w);
        // newRotation= orgRot ;
        // newRotation = float4(1,0,1,1);
        // p.y += 1;
    }

    if (CoordinateSpace < 0.5)
    {
        p.xyz = rotate_vector(p.xyz, orgRot).xyz;
        p += pOrg;
    }

    ResultPoints[i.x].position = p.xyz;
    ResultPoints[i.x].rotation = newRotation;

    float orgW = SourcePoints[i.x].w;
    ResultPoints[i.x].w = lerp(orgW, orgW * ScaleW + OffsetW, weight);
}
