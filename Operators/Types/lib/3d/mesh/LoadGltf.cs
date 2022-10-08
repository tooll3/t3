using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpDX;
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

namespace T3.Operators.Types.Id_92b18d2b_1022_488f_ab8e_a4dcca346a23
{
    public class LoadGltf : Instance<LoadGltf>
                          , IDescriptiveGraphNode
    {
        [Output(Guid = "47588d3a-28fe-4417-9ffc-2e79e59d2540")]
        public readonly Slot<MeshBuffers> Data = new Slot<MeshBuffers>();

        public LoadGltf()
        {
            Data.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var path = Path.GetValue(context);
            // var useGpuCaching = UseGPUCaching.GetValue(context);

            var childIndex = ChildIndex.GetValue(context);

            if (path != _lastFilePath)
            {
                int verticesCount = 0;
                int faceCount = 0;
                _lastFilePath = path;
                if (File.Exists(path))
                {
                    var fullPath = System.IO.Path.GetFullPath(path);
                    try
                    {
                        var resourceManager = ResourceManager.Instance();
                        var newData = new DataSet();

                        var model = SharpGLTF.Schema2.ModelRoot.Load(fullPath);


                        var children = model.DefaultScene.VisualChildren.ToList();
                        if (childIndex < 0 && childIndex >= children.Count)
                        {
                            Log.Warning($"gltf child index {childIndex} exceeds visible children in default scene {children.Count}");
                        }
                        else
                        {
                            var child = children[childIndex];
                            
                            // Create Vertex buffer
                            {
                                var vertexAccessors = child.Mesh.Primitives[0].VertexAccessors;
                                var keys = vertexAccessors.Keys;
                                var keys2 = string.Join(",", keys);
                                Log.Debug($"found attributes :{keys2}");
                                
                                if (!vertexAccessors.TryGetValue("POSITION", out var positionAccessor))
                                {
                                    Log.Warning("Can't find POSITION attribute in gltf mesh");
                                }
                                else
                                {
                                    verticesCount = positionAccessor.Count;

                                    if (newData.VertexBufferData.Length != verticesCount)
                                        newData.VertexBufferData = new PbrVertex[verticesCount];
                                    
                                    var positions = positionAccessor.AsVector3Array();
                                    int vertexIndex = 0;
                                    
                                    System.Numerics.Vector3[] normals = null;
                                    if (vertexAccessors.TryGetValue("NORMAL", out var normalAccess))
                                    {
                                        normals = normalAccess.AsVector3Array().ToArray();
                                    }

                                    System.Numerics.Vector2[] texCoords = null;
                                    if (vertexAccessors.TryGetValue("TEXCOORD_0", out var texAccess))
                                    {
                                        texCoords = texAccess.AsVector2Array().ToArray();
                                    }

                                    
                                    foreach (var position in positions)
                                    {
                                        newData.VertexBufferData[vertexIndex] = new PbrVertex
                                                                                    {
                                                                                        Position = new Vector3(position.X, position.Y, position.Z),
                                                                                        Normal = normals == null ? Vector3.Up : normals[vertexIndex].ToSharpDx(),
                                                                                        Tangent = Vector3.Right,
                                                                                        Bitangent = Vector3.ForwardLH,
                                                                                        Texcoord = texCoords == null ? Vector2.Zero : new Vector2(texCoords[vertexIndex].X,texCoords[vertexIndex].Y) ,
                                                                                        Selection = 1,
                                                                                    };
                                        vertexIndex++;
                                    }
                                }
                            }
                            
                            // Create Index buffer
                            {
                                var indices = child.Mesh.Primitives[0].GetTriangleIndices().ToList();
                                faceCount = indices.Count;
                                if (newData.IndexBufferData.Length != faceCount)
                                    newData.IndexBufferData = new SharpDX.Int3[faceCount];

                                var faceIndex = 0;
                                foreach (var (a, b, c) in indices)
                                {
                                    newData.IndexBufferData[faceIndex] = new SharpDX.Int3(a, b, c);
                                    
                                    // Calc TBN space
                                    var aPos = newData.VertexBufferData[a].Position;
                                    var aUV = newData.VertexBufferData[a].Texcoord;
                                    var aNormal = newData.VertexBufferData[a].Normal;
                                    
                                    var bPos = newData.VertexBufferData[b].Position;
                                    var bUV = newData.VertexBufferData[b].Texcoord;
                                    var bNormal = newData.VertexBufferData[b].Normal;
                                    
                                    var cPos = newData.VertexBufferData[c].Position;
                                    var cUV = newData.VertexBufferData[c].Texcoord;
                                    var cNormal = newData.VertexBufferData[c].Normal;
                                    
                                    MeshUtils.CalcTBNSpace(aPos, aUV, bPos, bUV, cPos, cUV, aNormal, out newData.VertexBufferData[a].Tangent, out newData.VertexBufferData[a].Bitangent);
                                    MeshUtils.CalcTBNSpace(bPos, bUV, cPos, cUV, aPos, aUV, bNormal, out newData.VertexBufferData[b].Tangent, out newData.VertexBufferData[b].Bitangent);
                                    MeshUtils.CalcTBNSpace(cPos, cUV, bPos, bUV, aPos, aUV, cNormal, out newData.VertexBufferData[c].Tangent, out newData.VertexBufferData[c].Bitangent);
                                    
                                    faceIndex++;
                                }

                                newData.IndexBufferWithViews.Buffer = newData.IndexBuffer;
                                const int stride = 3 * 4;
                                if (faceCount == 0)
                                {
                                    Log.Warning("No faces found");
                                    return;
                                }
                                ResourceManager.SetupStructuredBuffer(newData.IndexBufferData, stride * faceCount, stride, ref newData.IndexBuffer);
                                ResourceManager.CreateStructuredBufferSrv(newData.IndexBuffer, ref newData.IndexBufferWithViews.Srv);
                                ResourceManager.CreateStructuredBufferUav(newData.IndexBuffer, UnorderedAccessViewBufferFlags.None,
                                                                          ref newData.IndexBufferWithViews.Uav);
                            }



                            if (verticesCount == 0)
                            {
                                Log.Warning("No vertices found");
                                return;
                            }
                            Log.Debug($"  loaded {path} Child {childIndex}:  {verticesCount} vertices  {faceCount} faces");
                            newData.VertexBufferWithViews.Buffer = newData.VertexBuffer;
                            ResourceManager.SetupStructuredBuffer(newData.VertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride,
                                                                  ref newData.VertexBuffer);
                            ResourceManager.CreateStructuredBufferSrv(newData.VertexBuffer, ref newData.VertexBufferWithViews.Srv);
                            ResourceManager.CreateStructuredBufferUav(newData.VertexBuffer, UnorderedAccessViewBufferFlags.None,
                                                                      ref newData.VertexBufferWithViews.Uav);

                            _description = System.IO.Path.GetFileName(path);
                        }
                        _data = newData;
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Failed loading {path}: {e.Message}");
                    }
                }
                
                _data.DataBuffers.VertexBuffer = _data.VertexBufferWithViews;
                _data.DataBuffers.IndicesBuffer = _data.IndexBufferWithViews;
                Data.Value = _data.DataBuffers;
            }
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

        [Input(Guid = "6e0fa62a-a8e1-4d0b-a5b1-80876ff636c0")]
        public readonly InputSlot<string> Path = new();

        // [Input(Guid = "d9843d5b-1935-4028-b8f7-620fae494981")]
        // public readonly InputSlot<bool> UseGPUCaching = new();
        //
        // [Input(Guid = "67b02db2-2313-483f-b2aa-e46282877f5f")]
        // public readonly InputSlot<bool> ClearGPUCache = new();
        //
        [Input(Guid = "38232F5A-71A6-4213-BF04-AB80A0E448DB")]
        public readonly InputSlot<int> ChildIndex = new();
    }
}