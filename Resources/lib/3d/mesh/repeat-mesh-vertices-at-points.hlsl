#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float UseWForSize;
    float Size;
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
    //PbrVertex vertex = SourceVertices[FaceIndices[faceIndex][faceVertexIndex]];
    float4 posInObject = float4( v.Position,1);

    float4x4 orientationMatrix = transpose(quaternion_to_matrix(p.rotation));

    posInObject.xyz *= (UseWForSize ? p.w : 1) * Size;
    posInObject = mul( float4(posInObject.xyz, 1), orientationMatrix) ;

    posInObject += float4(p.position, 0); 

    v.Position = posInObject; 
    ResultVertices[targetVertexIndex] = v; 
}

