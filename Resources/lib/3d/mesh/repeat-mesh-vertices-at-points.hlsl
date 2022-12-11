#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float3 Stretch;
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
    float4 posInObject = float4( v.Position,1);

    float4x4 orientationMatrix = transpose(quaternion_to_matrix(p.rotation));

    posInObject.xyz *= Size;
    posInObject.xyz *= UseWForSize ? (lerp(Size, Size + p.w,  Stretch) ) :1;
    //posInObject.xyz *= (UseWForSize ? (lerp(Size, Size + p.w,  Stretch) ) : Size);
    posInObject = mul( float4(posInObject.xyz, 1), orientationMatrix) ;

    posInObject += float4(p.position, 0); 

    v.Position = posInObject; 
    v.Normal = rotate_vector(v.Normal, p.rotation);
    v.Tangent = rotate_vector(v.Tangent, p.rotation);
    v.Bitangent = rotate_vector(v.Bitangent, p.rotation);
    ResultVertices[targetVertexIndex] = v; 
}

