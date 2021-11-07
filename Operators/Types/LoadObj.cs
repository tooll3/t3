using System.Collections.Generic;
using System.IO;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_be52b670_9749_4c0d_89f0_d8b101395227
{
    public class LoadObj : Instance<LoadObj>, IDescriptiveGraphNode
    {
        [Output(Guid = "1F4E7CAC-1F62-4633-B0F3-A3017A026753")]
        public readonly Slot<MeshBuffers> Data = new Slot<MeshBuffers>();

        public LoadObj()
        {
            Data.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var path = Path.GetValue(context);
            if (path != _lastFilePath || SortVertices.DirtyFlag.IsDirty)
            {
                var vertexSorting = SortVertices.GetValue(context);
                _description = System.IO.Path.GetFileName(path);

                if (UseGPUCaching.GetValue(context))
                {
                    if (MeshBufferCache.TryGetValue(path, out var cachedBuffer))
                    {
                        Data.Value = cachedBuffer.DataBuffers;
                        return;
                    }
                }

                var mesh = ObjMesh.LoadFromFile(path);
                if (mesh == null || mesh.DistinctDistinctVertices.Count == 0)
                {
                    Log.Warning($"Can't read file {path}");
                    return;
                }

                mesh.UpdateVertexSorting((ObjMesh.SortDirections)vertexSorting);

                _lastFilePath = path;

                var resourceManager = ResourceManager.Instance();

                var newData = new DataSet();

                var reversedLookup = new int[mesh.DistinctDistinctVertices.Count];

                // Create Vertex buffer
                {
                    var verticesCount = mesh.DistinctDistinctVertices.Count;
                    if (newData.VertexBufferData.Length != verticesCount)
                        newData.VertexBufferData = new PbrVertex[verticesCount];

                    for (var vertexIndex = 0; vertexIndex < verticesCount; vertexIndex++)
                    {
                        var sortedVertexIndex = mesh.SortedVertexIndices[vertexIndex];
                        var sortedVertex = mesh.DistinctDistinctVertices[sortedVertexIndex];
                        reversedLookup[sortedVertexIndex] = vertexIndex;
                        newData.VertexBufferData[vertexIndex] = new PbrVertex
                                                                    {
                                                                        Position = mesh.Positions[sortedVertex.PositionIndex],
                                                                        Normal = mesh.Normals[sortedVertex.NormalIndex],
                                                                        Tangent = mesh.VertexTangents[sortedVertexIndex],
                                                                        Bitangent = mesh.VertexBinormals[sortedVertexIndex],
                                                                        Texcoord = mesh.TexCoords[sortedVertex.TextureCoordsIndex],
                                                                        Selection = 1,
                                                                    };
                    }

                    newData.VertexBufferWithViews.Buffer = newData.VertexBuffer;
                    resourceManager.SetupStructuredBuffer(newData.VertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride,
                                                          ref newData.VertexBuffer);
                    resourceManager.CreateStructuredBufferSrv(newData.VertexBuffer, ref newData.VertexBufferWithViews.Srv);
                    resourceManager.CreateStructuredBufferUav(newData.VertexBuffer, UnorderedAccessViewBufferFlags.None, ref newData.VertexBufferWithViews.Uav);
                }

                // Create Index buffer
                {
                    var faceCount = mesh.Faces.Count;
                    if (newData.IndexBufferData.Length != faceCount)
                        newData.IndexBufferData = new SharpDX.Int3[faceCount];

                    for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
                    {
                        var face = mesh.Faces[faceIndex];
                        var v1Index = mesh.GetVertexIndex(face.V0, face.V0n, face.V0t);
                        var v2Index = mesh.GetVertexIndex(face.V1, face.V1n, face.V1t);
                        var v3Index = mesh.GetVertexIndex(face.V2, face.V2n, face.V2t);

                        newData.IndexBufferData[faceIndex]
                            = new SharpDX.Int3(reversedLookup[v1Index],
                                               reversedLookup[v2Index],
                                               reversedLookup[v3Index]);
                    }

                    newData.IndexBufferWithViews.Buffer = newData.IndexBuffer;
                    const int stride = 3 * 4;
                    resourceManager.SetupStructuredBuffer(newData.IndexBufferData, stride * faceCount, stride, ref newData.IndexBuffer);
                    resourceManager.CreateStructuredBufferSrv(newData.IndexBuffer, ref newData.IndexBufferWithViews.Srv);
                    resourceManager.CreateStructuredBufferUav(newData.IndexBuffer, UnorderedAccessViewBufferFlags.None, ref newData.IndexBufferWithViews.Uav);
                }

                if (UseGPUCaching.GetValue(context))
                {
                    MeshBufferCache[path] = newData;
                }

                _data = newData;
            }

            _data.DataBuffers.VertexBuffer = _data.VertexBufferWithViews;
            _data.DataBuffers.IndicesBuffer = _data.IndexBufferWithViews;
            Data.Value = _data.DataBuffers;
        }

        public string GetDescriptiveString()
        {
            return _description;
        }

        private string _description;
        private string _lastFilePath;
        private DataSet _data = new DataSet();

        private class DataSet
        {
            public readonly MeshBuffers DataBuffers = new MeshBuffers();

            public Buffer VertexBuffer;
            public PbrVertex[] VertexBufferData = new PbrVertex[0];
            public readonly BufferWithViews VertexBufferWithViews = new BufferWithViews();

            public Buffer IndexBuffer;
            public SharpDX.Int3[] IndexBufferData = new SharpDX.Int3[0];
            public readonly BufferWithViews IndexBufferWithViews = new BufferWithViews();
        }

        private static readonly Dictionary<string, DataSet> MeshBufferCache = new Dictionary<string, DataSet>();

        [Input(Guid = "7d576017-89bd-4813-bc9b-70214efe6a27")]
        public readonly InputSlot<string> Path = new InputSlot<string>();

        [Input(Guid = "FFD22736-A600-4C97-A4A4-AD3526B8B35C")]
        public readonly InputSlot<bool> UseGPUCaching = new InputSlot<bool>();

        [Input(Guid = "DDD22736-A600-4C97-A4A4-AD3526B8B35C")]
        public readonly InputSlot<bool> ClearGPUCache = new InputSlot<bool>();

        
        [Input(Guid = "AA19E71D-329C-448B-901C-565BF8C0DA4F", MappedType = typeof(ObjMesh.SortDirections))]
        public readonly InputSlot<int> SortVertices = new InputSlot<int>();
    }
}