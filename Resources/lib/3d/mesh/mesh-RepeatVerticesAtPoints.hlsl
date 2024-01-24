#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float3 Stretch;
    float UseWForSize;
    float Size;
    float UseStretch;
}

StructuredBuffer<PbrVertex> SourceVertices : t0;       
StructuredBuffer<Point> Points : t1;       

RWStructuredBuffer<PbrVertex> ResultVertices : u0;   


[numthreads(16,16,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint vertexIndex = i.x;
    uint pointIndex = i.y;
    uint sourcePointCount, sourceVertexCount, stride;

    Points.GetDimensions(sourcePointCount, stride);
    SourceVertices.GetDimensions(sourceVertexCount, stride);

    if(pointIndex >= sourcePointCount || vertexIndex >= sourceVertexCount) {
        return;
    }
    
    int targetVertexIndex = pointIndex * sourceVertexCount + vertexIndex;

    PbrVertex v = SourceVertices[vertexIndex];
    
    Point p = Points[pointIndex];

    // Apply point transform
    float4 posInObject = float4( v.Position,1);

    float4x4 orientationMatrix = transpose(qToMatrix(p.Rotation));

    //posInObject.xyz *= Size;
    
    //posInObject.xyz *= UseWForSize ?  Size * p.W +  Stretch :1;
    //posInObject.xyz *= UseStretch ? Size * p.Stretch : 1;
    //posInObject.xyz *= (UseWForSize ? (lerp(Size, Size + p.w,  Stretch) ) : Size);

    float3 resizeFromW = UseWForSize ?  p.W *  Stretch :1;
    float3 resizeFromStretch = UseStretch ?  p.Stretch : 1;
    posInObject.xyz *= max(0, resizeFromW) * Size * resizeFromStretch;

    posInObject = mul( float4(posInObject.xyz, 1), orientationMatrix) ;

    posInObject += float4(p.Position, 0); 

    v.Position = posInObject; 
    v.Normal = qRotateVec3(v.Normal, p.Rotation);
    v.Tangent = qRotateVec3(v.Tangent, p.Rotation);
    v.Bitangent = qRotateVec3(v.Bitangent, p.Rotation);
    ResultVertices[targetVertexIndex] = v; 
}

