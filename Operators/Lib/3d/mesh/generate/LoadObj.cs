using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
// ReSharper disable RedundantNameQualifier

namespace lib._3d.mesh.generate
{
	[Guid("be52b670-9749-4c0d-89f0-d8b101395227")]
    public class LoadObj : Instance<LoadObj>, IDescriptiveFilename, IStatusProvider
    {
        [Output(Guid = "1F4E7CAC-1F62-4633-B0F3-A3017A026753")]
        public readonly Slot<MeshBuffers> Data = new();

        public LoadObj()
        {
            Data.UpdateAction = Update;
        }

        private float _scaleFactor;
        
        private void Update(EvaluationContext context)
        {
            var path = Path.GetValue(context);
            var vertexSorting = SortVertices.GetEnumValue<ObjMesh.SortDirections>(context);
            var useGpuCaching = UseGPUCaching.GetValue(context);
            var scaleFactor = ScaleFactor.GetValue(context);
            if (ClearGPUCache.GetValue(context))
            {
                _meshBufferCache.Clear();
            }
            
            if (_sourceFileChanged ||  path != _lastFilePath 
                                   || SortVertices.DirtyFlag.IsDirty 
                                   || Math.Abs(scaleFactor - _scaleFactor) > 0.001f
                                   || vertexSorting != _lastSorting)
            {
                if (!TryGetFilePath(path, out var absolutePath))
                {
                    Log.Error($"File not found: {path}", this);
                    _warningMessage = $"File not found: {path}";
                    return;
                }
                
                ResourceFileWatcher.AddFileHook(absolutePath, FileChangedHandler);
                _sourceFileChanged = false;
                _lastSorting = vertexSorting;
                _scaleFactor = scaleFactor;
                
                if (useGpuCaching)
                {
                    if (_meshBufferCache.TryGetValue(absolutePath, out var cachedBuffer))
                    {
                        Data.Value = cachedBuffer.DataBuffers;
                        return;
                    }
                }

                var mesh = ObjMesh.LoadFromFile(absolutePath);
                if (mesh == null || mesh.DistinctDistinctVertices.Count == 0)
                {
                    var warningMessage = $"Can't read file {absolutePath}";
                    Log.Warning(warningMessage, this);
                    _warningMessage = warningMessage;
                    return;
                }

                mesh.UpdateVertexSorting(vertexSorting);

                _lastFilePath = path;

                var newData = new MeshDataSet();
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
                                                                        Position = mesh.Positions[sortedVertex.PositionIndex] * scaleFactor,
                                                                        Normal = mesh.Normals[sortedVertex.NormalIndex],
                                                                        Tangent = mesh.VertexTangents[sortedVertexIndex],
                                                                        Bitangent = mesh.VertexBinormals[sortedVertexIndex],
                                                                        Texcoord = mesh.TexCoords[sortedVertex.TextureCoordsIndex],
                                                                        Selection = 1,
                                                                    };
                    }

                    ResourceManager.SetupStructuredBuffer(newData.VertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride,
                                                          ref newData.VertexBuffer);
                    ResourceManager.CreateStructuredBufferSrv(newData.VertexBuffer, ref newData.VertexBufferWithViews.Srv);
                    ResourceManager.CreateStructuredBufferUav(newData.VertexBuffer, UnorderedAccessViewBufferFlags.None, ref newData.VertexBufferWithViews.Uav);
                    newData.VertexBufferWithViews.Buffer = newData.VertexBuffer;
                }

                // Create Index buffer
                {
                    var faceCount = mesh.Faces.Count;
                    if (newData.IndexBufferData.Length != faceCount)
                        newData.IndexBufferData = new Int3[faceCount];

                    for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
                    {
                        var face = mesh.Faces[faceIndex];
                        var v1Index = mesh.GetVertexIndex(face.V0, face.V0n, face.V0t);
                        var v2Index = mesh.GetVertexIndex(face.V1, face.V1n, face.V1t);
                        var v3Index = mesh.GetVertexIndex(face.V2, face.V2n, face.V2t);

                        newData.IndexBufferData[faceIndex]
                            = new Int3(reversedLookup[v1Index],
                                               reversedLookup[v2Index],
                                               reversedLookup[v3Index]);
                    }

                    const int stride = 3 * 4;
                    ResourceManager.SetupStructuredBuffer(newData.IndexBufferData, stride * faceCount, stride, ref newData.IndexBuffer);
                    ResourceManager.CreateStructuredBufferSrv(newData.IndexBuffer, ref newData.IndexBufferWithViews.Srv);
                    ResourceManager.CreateStructuredBufferUav(newData.IndexBuffer, UnorderedAccessViewBufferFlags.None, ref newData.IndexBufferWithViews.Uav);
                    newData.IndexBufferWithViews.Buffer = newData.IndexBuffer;
                }

                if (useGpuCaching)
                {
                    _meshBufferCache[absolutePath] = newData;
                }

                _meshData = newData;
            }

            _meshData.DataBuffers.VertexBuffer = _meshData.VertexBufferWithViews;
            _meshData.DataBuffers.IndicesBuffer = _meshData.IndexBufferWithViews;
            Data.Value = _meshData.DataBuffers;
            _warningMessage = null;
        }

        private void FileChangedHandler()
        {
            Path.DirtyFlag.Invalidate();
            _sourceFileChanged = true;
        }

        public InputSlot<string> GetSourcePathSlot()
        {
            return Path;
        }

        private bool _sourceFileChanged;
        private string _lastFilePath;
        private ObjMesh.SortDirections _lastSorting;
        private MeshDataSet _meshData = new();

        private class MeshDataSet
        {
            public readonly MeshBuffers DataBuffers = new();

            public Buffer VertexBuffer;
            public PbrVertex[] VertexBufferData = Array.Empty<PbrVertex>();
            public readonly BufferWithViews VertexBufferWithViews = new();

            public Buffer IndexBuffer;
            public Int3[] IndexBufferData = Array.Empty<Int3>();
            public readonly BufferWithViews IndexBufferWithViews = new();
        }

        
        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_warningMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _warningMessage;
        }

        private string _warningMessage;
        
        
        private static readonly Dictionary<string, MeshDataSet> _meshBufferCache = new();

        [Input(Guid = "7d576017-89bd-4813-bc9b-70214efe6a27")]
        public readonly InputSlot<string> Path = new();

        [Input(Guid = "FFD22736-A600-4C97-A4A4-AD3526B8B35C")]
        public readonly InputSlot<bool> UseGPUCaching = new();

        [Input(Guid = "DDD22736-A600-4C97-A4A4-AD3526B8B35C")]
        public readonly InputSlot<bool> ClearGPUCache = new();
        
        [Input(Guid = "AA19E71D-329C-448B-901C-565BF8C0DA4F", MappedType = typeof(ObjMesh.SortDirections))]
        public readonly InputSlot<int> SortVertices = new();
        
        [Input(Guid = "C39A61B3-FB6B-4611-8F13-273F13C9C491")]
        public readonly InputSlot<float> ScaleFactor = new();


    }
}