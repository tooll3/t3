#include "shared/pbr.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
}

static const int IndicesCount = 15;

struct FaceCount {
    int Count;
    int Indices[IndicesCount];
};


StructuredBuffer<PbrVertex> SourceVertices : register(t0);        
StructuredBuffer<int3> SourceFaces : register(t1);        

RWStructuredBuffer<PbrVertex> ResultVertices : register(u0);   
RWStructuredBuffer<FaceCount> VertexFaces : register(u1);   

[numthreads(64,1,1)]
void clear(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    VertexFaces[gi].Count = 0;
    for(int index=0; index < IndicesCount; ++index) 
    {
        VertexFaces[gi].Indices[index] = 0;
    } 
}

[numthreads(64,1,1)]
void registerFaceVertices(uint3 i : SV_DispatchThreadID)
{
    uint faceIndex = i.x; 
    uint faceCount, _;
    SourceFaces.GetDimensions(faceCount, _);
    if(faceIndex >= faceCount)
        return;
    
    float tmp = SourceVertices[0].Normal;
    int3 verticeIndices = SourceFaces[i.x];

    for(int side=0; side<3 ;++side) 
    {
        int vIndex = verticeIndices[side];
        int orgValue = VertexFaces[vIndex].Count;
        InterlockedAdd( VertexFaces[vIndex].Count, 1, orgValue);
        if(orgValue > IndicesCount)
            return;

        VertexFaces[vIndex].Indices[orgValue]= faceIndex;
    }
}


[numthreads(64,1,1)]
void computeNormal(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    uint count, _;
    ResultVertices.GetDimensions(count, _);
    if(gi >= count)
        return;

    int faceCount = VertexFaces[gi].Count;
    if(faceCount == 0)
        return;

    float3 normalSum = 0;
    float3 center = SourceVertices[gi].Position;

    float3 sidePositions[2];

    for(int face=0; face < faceCount && face <= IndicesCount; ++face) 
    {
        uint faceIndex = VertexFaces[gi].Indices[face];
        uint3 faceVertexIndices = SourceFaces[faceIndex];

        int sideIndex =0;
        for(int side=0; side< 3; ++side) 
        {
            int vIndex = faceVertexIndices[side];
            if(vIndex == gi)
                continue;

            sidePositions[sideIndex] = SourceVertices[faceVertexIndices[side]].Position;
            sideIndex++;
        }

        float3 p1 = sidePositions[0];
        float3 p2 = sidePositions[1];
        normalSum += cross(normalize( p2 - p1 ), normalize(center - p1) );
        normalSum += cross(normalize(center - p2), normalize( p1 - p2 ) );
    }
    
    ResultVertices[gi] = SourceVertices[gi];

    float3 bitangent = SourceVertices[gi].Bitangent;
    float3 tangent =  SourceVertices[gi].Tangent;
    float3 newNormal = normalize( normalSum );

    float3 newTangent = cross(bitangent,newNormal);
    float3 newBitangent = cross(newNormal, newTangent);

    ResultVertices[gi].Selected = faceCount;
    ResultVertices[gi].Normal = newNormal;
    ResultVertices[gi].Tangent = newTangent;
    ResultVertices[gi].Bitangent = newBitangent;
}