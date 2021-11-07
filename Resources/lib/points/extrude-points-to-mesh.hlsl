#include "hash-functions.hlsl"
#include "noise-functions.hlsl"
#include "point.hlsl"
#include "pbr.hlsl"

cbuffer Params : register(b0)
{
    // float SmoothDistance;
    // float SampleMode;
    // float2 SampleRange;
}

StructuredBuffer<Point> RailPoints : t0;
StructuredBuffer<Point> ShapePoints : t1;

RWStructuredBuffer<PbrVertex> Vertices : u0;
RWStructuredBuffer<int3> TriangleIndices : u1;



[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{

    uint triangleCount, stride;
    TriangleIndices.GetDimensions(triangleCount, stride);

    uint vertexCount;
    Vertices.GetDimensions(vertexCount, stride);

    if(i.x >= vertexCount) {
        return;
    }

    

    uint rows;
    ShapePoints.GetDimensions(rows, stride);

    uint columns;
    RailPoints.GetDimensions(columns, stride);

    uint vertexIndex = i.x;
    uint rowIndex = vertexIndex % rows;
    uint columnIndex = vertexIndex / rows;

    PbrVertex v;
    Point railPoint = RailPoints[columnIndex];
    Point shapePoint = ShapePoints[rowIndex];

    float4 rotation = normalize(qmul(shapePoint.rotation, railPoint.rotation ));
    float3 position = rotate_vector(shapePoint.position * railPoint.w, railPoint.rotation) + railPoint.position;

    v.Position =  position;
    v.Normal = rotate_vector(float3(0,0,1), rotation);
    v.Tangent = rotate_vector(float3(1,0,0), rotation);
    v.Bitangent = rotate_vector(float3(0,1,0), rotation);
    v.TexCoord = float2((float)columnIndex/(columns-1),(float)rowIndex/(rows-1));
    v.Selected = 1;
    v.__padding =0;

    Vertices[vertexIndex] = v;
    if(isnan(railPoint.w ) || isnan(shapePoint.w) )
        Vertices[vertexIndex].Position = float3(0,0,0);
    

    // Write face indices
    if (columnIndex < columns - 1 && rowIndex < rows - 1) 
    {
        int faceIndex =  2 * (rowIndex + columnIndex * (rows-1));

        if(
            isnan(railPoint.w) 
            || isnan(RailPoints[columnIndex+1].w) 
            || isnan(shapePoint.w) 
            || isnan(ShapePoints[rowIndex+1].w) 
         ) 
        {
            if (columnIndex < columns - 1 && rowIndex < rows - 1) 
            {
                TriangleIndices[faceIndex + 0] = int3(0, 0, 0);
                TriangleIndices[faceIndex + 1] = int3(0, 0, 0);
                TriangleIndices[faceIndex + 1] = int3(0, 0, 0);
            }
             if(isnan(railPoint.w ) || isnan(shapePoint.w) )
                 Vertices[vertexIndex].Position = float3(0,0,0);
            return;
        }        
        TriangleIndices[faceIndex + 0] = int3(vertexIndex+1, vertexIndex + rows, vertexIndex );
        TriangleIndices[faceIndex + 1] = int3(vertexIndex + 1, vertexIndex + rows+1, vertexIndex + rows);
    }
}

