using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SharpDX.Direct3D11;
using SharpGLTF.Schema2;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Resource;
using T3.Core.Utils.Geometry;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_92b18d2b_1022_488f_ab8e_a4dcca346a23;

public class LoadGltf : Instance<LoadGltf>
                      , IDescriptiveFilename, IStatusProvider
{
    [Output(Guid = "47588d3a-28fe-4417-9ffc-2e79e59d2540")]
    public readonly Slot<MeshBuffers> Data = new();

    public LoadGltf()
    {
        Data.UpdateAction = Update;
    }

    private void Update(EvaluationContext context)
    {
        var pathChanged = Path.DirtyFlag.IsDirty;
        var childIndexChanged = ChildIndex.DirtyFlag.IsDirty;

        var childIndex = ChildIndex.GetValue(context);

        if (pathChanged || childIndexChanged)
        {
            var path = Path.GetValue(context);
            if (!File.Exists(path))
            {
                ShowError($"Gltf File not found: {path}");
                return;
            }

            var fullPath = System.IO.Path.GetFullPath(path);
            var model = SharpGLTF.Schema2.ModelRoot.Load(fullPath);
            
            if(childIndexChanged)
                UpdateBuffers(model, childIndex);
            
        }
    }

    private void ShowError(string message)
    {
        _lastErrorMessage = message;
        Log.Warning(_lastErrorMessage, this);
    }

    private void UpdateBuffers(ModelRoot model, int childIndex)
    {
        var children = model.DefaultScene.VisualChildren.ToList();
        if (childIndex < 0 || childIndex >= children.Count)
        {
            ShowError($"gltf child index {childIndex} exceeds visible children in default scene {children.Count}");
            return;
        }

        var child = children[childIndex];
        Data.Value = GenerateMeshDataFromGltfChild(child);
    }
    
    private MeshBuffers GenerateMeshDataFromGltfChild(Node child)
    {
        //var newData = new GltfDataSet();
        var newMesh = new MeshBuffers();
        
        var vertexBufferData = Array.Empty<PbrVertex>();
        var indexBufferData = Array.Empty<Int3>();

        try
        {
            // Convert vertices
            int verticesCount = 0;
            {
                var vertexAccessors = child.Mesh.Primitives[0].VertexAccessors;
                var keys = vertexAccessors.Keys;
                var keys2 = string.Join(",", keys);
                Log.Debug($"found attributes :{keys2}", this);

                // Collect positions
                if (!vertexAccessors.TryGetValue("POSITION", out var positionAccessor))
                {
                    ShowError("Can't find POSITION attribute in gltf mesh");
                    return null;
                }

                verticesCount = positionAccessor.Count;

                if (vertexBufferData.Length != verticesCount)
                    vertexBufferData = new PbrVertex[verticesCount];

                var positions = positionAccessor.AsVector3Array();

                // Collect normals
                Vector3[] normals = null;
                if (vertexAccessors.TryGetValue("NORMAL", out var normalAccess))
                {
                    normals = normalAccess.AsVector3Array().ToArray();
                }

                // Collect texture coords
                Vector2[] texCoords = null;
                if (vertexAccessors.TryGetValue("TEXCOORD_0", out var texAccess))
                {
                    texCoords = texAccess.AsVector2Array().ToArray();
                }

                // Write vertex buffer
                for (var vertexIndex = 0; vertexIndex < positions.Count; vertexIndex++)
                {
                    var position = positions[vertexIndex];
                    vertexBufferData[vertexIndex] = new PbrVertex
                                                                {
                                                                    Position = new Vector3(position.X, position.Y, position.Z),
                                                                    Normal = normals == null ? VectorT3.Up : normals[vertexIndex],
                                                                    Tangent = VectorT3.Right,
                                                                    Bitangent = VectorT3.ForwardLH,
                                                                    Texcoord = texCoords == null
                                                                                   ? Vector2.Zero
                                                                                   : new Vector2(texCoords[vertexIndex].X,
                                                                                                 1-texCoords[vertexIndex].Y),
                                                                    Selection = 1,
                                                                };
                }
            }

            // Convert indices
            int faceCount;
            {
                var indices = child.Mesh.Primitives[0].GetTriangleIndices().ToList();
                faceCount = indices.Count;
                if (indexBufferData.Length != faceCount)
                    indexBufferData = new Int3[faceCount];

                var faceIndex = 0;
                foreach (var (a, b, c) in indices)
                {
                    indexBufferData[faceIndex] = new Int3(a, b, c);

                    // Calc TBN space
                    var aPos = vertexBufferData[a].Position;
                    var aUV = vertexBufferData[a].Texcoord;
                    var aNormal = vertexBufferData[a].Normal;

                    var bPos = vertexBufferData[b].Position;
                    var bUV = vertexBufferData[b].Texcoord;
                    var bNormal = vertexBufferData[b].Normal;

                    var cPos = vertexBufferData[c].Position;
                    var cUV = vertexBufferData[c].Texcoord;
                    var cNormal = vertexBufferData[c].Normal;

                    MeshUtils.CalcTBNSpace(aPos, aUV, bPos, bUV, cPos, cUV, aNormal, out vertexBufferData[a].Tangent,
                                           out vertexBufferData[a].Bitangent);
                    MeshUtils.CalcTBNSpace(bPos, bUV, cPos, cUV, aPos, aUV, bNormal, out vertexBufferData[b].Tangent,
                                           out vertexBufferData[b].Bitangent);
                    MeshUtils.CalcTBNSpace(cPos, cUV, bPos, bUV, aPos, aUV, cNormal, out vertexBufferData[c].Tangent,
                                           out vertexBufferData[c].Bitangent);

                    faceIndex++;
                }

                if (faceCount == 0)
                {
                    Log.Warning("No faces found", this);
                    return null;
                }
            }

            if (verticesCount == 0)
            {
                Log.Warning("No vertices found", this);
                return null;
            }

            Log.Debug($"  loaded gltf-child:  {verticesCount} vertices  {faceCount} faces", this);

            const int stride = 3 * 4;
            ResourceManager.SetupStructuredBuffer(indexBufferData, stride * faceCount, stride, ref newMesh.IndicesBuffer.Buffer);
            ResourceManager.CreateStructuredBufferSrv(newMesh.IndicesBuffer.Buffer, ref newMesh.IndicesBuffer.Srv);
            ResourceManager.CreateStructuredBufferUav(newMesh.IndicesBuffer.Buffer, UnorderedAccessViewBufferFlags.None,
                                                      ref newMesh.IndicesBuffer.Uav);

            ResourceManager.SetupStructuredBuffer(vertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride,
                                                  ref newMesh.VertexBuffer.Buffer);
            ResourceManager.CreateStructuredBufferSrv(newMesh.VertexBuffer.Buffer, ref newMesh.VertexBuffer.Srv);
            ResourceManager.CreateStructuredBufferUav(newMesh.VertexBuffer.Buffer, UnorderedAccessViewBufferFlags.None,
                                                      ref newMesh.VertexBuffer.Uav);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed loading gltf: {e.Message}", this);
        }

        return newMesh;
    }
    
    

    public InputSlot<string> GetSourcePathSlot()
    {
        return Path;
    }

    [Input(Guid = "6e0fa62a-a8e1-4d0b-a5b1-80876ff636c0")]
    public readonly InputSlot<string> Path = new();

    [Input(Guid = "38232F5A-71A6-4213-BF04-AB80A0E448DB")]
    public readonly InputSlot<int> ChildIndex = new();

    public IStatusProvider.StatusLevel GetStatusLevel()
    {
        return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
    }

    public string GetStatusMessage()
    {
        return _lastErrorMessage;
    }

    private string _lastErrorMessage;
}