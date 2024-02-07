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
using Scene = SharpGLTF.Schema2.Scene;

namespace T3.Operators.Types.Id_00618c91_f39a_44ea_b9d8_175c996460dc;

public class LoadGltfScene : Instance<LoadGltfScene>
                           , IDescriptiveFilename, IStatusProvider
{
    [Output(Guid = "C2D28EBE-E235-4234-8234-EB56E87ED9AF")]
    public readonly Slot<SceneSetup> ResultSetup = new();

    public LoadGltfScene()
    {
        ResultSetup.UpdateAction = Update;
    }

    private SceneSetup _sceneSetup;

    private void Update(EvaluationContext context)
    {
        _lastErrorMessage = null;

        _sceneSetup = Setup.GetValue(context);
        if (_sceneSetup == null)
        {
            _sceneSetup = new SceneSetup();
            Setup.SetTypedInputValue(_sceneSetup);
        }

        var path = Path.GetValue(context);

        if (path != _lastFilePath || TriggerUpdate.GetValue(context))
        {
            _sceneSetup = new SceneSetup();
            Setup.SetTypedInputValue(_sceneSetup);
            
            _lastFilePath = path;

            if (!File.Exists(path))
            {
                ShowError($"Gltf File not found: {path}");
                return;
            }

            var fullPath = System.IO.Path.GetFullPath(path);
            var model = ModelRoot.Load(fullPath);

            var rootNode = ConvertToNodeStructure(model.DefaultScene);

            _sceneSetup.RootNodes.Clear();
            _sceneSetup.RootNodes.Add(rootNode);
            _sceneSetup.GenerateSceneDrawDispatches();

            ResultSetup.Value = _sceneSetup;
        }
    }

    private void ShowError(string message)
    {
        _lastErrorMessage = message;
        Log.Warning(_lastErrorMessage, this);
    }

    private SceneSetup.SceneNode ConvertToNodeStructure(Scene modelDefaultScene)
    {
        var rootNode = new SceneSetup.SceneNode()
                           {
                               Name = modelDefaultScene.Name
                           };

        ParseChildren(modelDefaultScene.VisualChildren, rootNode);
        return rootNode;
    }

    private void ParseChildren(IEnumerable<Node> visualChildren, SceneSetup.SceneNode parentNode)
    {
        foreach (var child in visualChildren)
        {
            var transform = new SceneSetup.Transform
                                {
                                    Translation = child.LocalTransform.Translation,
                                    Scale = child.LocalTransform.Scale,
                                    Rotation = child.LocalTransform.Rotation,
                                };

            // Pure logic node
            var structureNode = new SceneSetup.SceneNode()
                                    {
                                        Name = child.Name,
                                        // MeshName = child.Mesh?.Name,
                                        // MeshBuffers = meshBuffers,
                                        Transform = transform,
                                        CombinedTransform = child.WorldMatrix,
                                    };

            parentNode.ChildNodes.Add(structureNode);

            var useStructureNodeForMesh = true;

            if (child.Mesh != null)
            {
                foreach (var meshPrimitives in child.Mesh.Primitives)
                {
                    if (!TryGenerateMeshBuffersFromGltfChild(meshPrimitives, out var meshBuffers, out var errorMessage))
                    {
                        ShowError(errorMessage);
                        meshBuffers = null;
                    }

                    if (meshBuffers != null && useStructureNodeForMesh)
                    {
                        structureNode.MeshBuffers = meshBuffers;
                        useStructureNodeForMesh = false;
                        continue;
                    }

                    if (meshBuffers == null)
                        continue;

                    var meshNode = new SceneSetup.SceneNode()
                                       {
                                           Name = child.Name,
                                           MeshName = child.Mesh?.Name,
                                           MeshBuffers = meshBuffers,
                                           Transform = transform,
                                           CombinedTransform = child.WorldMatrix,
                                       };
                    parentNode.ChildNodes.Add(meshNode);
                }
            }

            ParseChildren(child.VisualChildren, structureNode);
        }
    }

    private bool TryGenerateMeshBuffersFromGltfChild(MeshPrimitive meshPrimitive, out MeshBuffers newMesh, out string message)
    {
        // TODO: return cached mesh to reuse buffer

        newMesh = new MeshBuffers();
        message = null;

        var vertexBufferData = Array.Empty<PbrVertex>();
        var indexBufferData = Array.Empty<Int3>();

        // if (child.Mesh == null)
        // {
        //     message = $"Child {child.Name} contains no mesh";
        //     return false;
        // }

        try
        {
            // Convert vertices
            var verticesCount = 0;
            {
                // TODO: Iterate over all primitives
                var vertexAccessors = meshPrimitive.VertexAccessors;
                var keys = vertexAccessors.Keys;
                var keys2 = string.Join(",", keys);
                Log.Debug($"found attributes :{keys2}", this);

                // Collect positions
                if (!vertexAccessors.TryGetValue("POSITION", out var positionAccessor))
                {
                    message = "Can't find POSITION attribute in gltf mesh";
                    return false;
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
                                                                                         texCoords[vertexIndex].Y),
                                                            Selection = 1,
                                                        };
                }
            }

            // Convert indices
            int faceCount;
            {
                var indices = meshPrimitive.GetTriangleIndices().ToList();
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

                    // MeshUtils.CalcTBNSpace(aPos, aUV, bPos, bUV, cPos, cUV, aNormal, out vertexBufferData[a].Tangent,
                    //                        out vertexBufferData[a].Bitangent);
                    // MeshUtils.CalcTBNSpace(bPos, bUV, cPos, cUV, aPos, aUV, bNormal, out vertexBufferData[b].Tangent,
                    //                        out vertexBufferData[b].Bitangent);
                    // MeshUtils.CalcTBNSpace(cPos, cUV, bPos, bUV, aPos, aUV, cNormal, out vertexBufferData[c].Tangent,
                    //                        out vertexBufferData[c].Bitangent);

                    faceIndex++;
                }

                if (faceCount == 0)
                {
                    message = "No faces found";
                    return false;
                }
            }

            if (verticesCount == 0)
            {
                message = "No vertices found";
                return false;
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
            message = $"Failed loading gltf: {e.Message}";
            return false;
        }

        return true;
    }

    #region implement graph node interfaces
    InputSlot<string> IDescriptiveFilename.GetSourcePathSlot()
    {
        return Path;
    }

    private string _lastFilePath;

    IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel()
    {
        return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
    }

    string IStatusProvider.GetStatusMessage()
    {
        return _lastErrorMessage;
    }

    private string _lastErrorMessage;
    #endregion

    [Input(Guid = "292e80cf-ba31-4a50-9bf4-83712430f811")]
    public readonly InputSlot<string> Path = new();

    [Input(Guid = "D02F41A6-1A6B-4A6E-8D6C-A28873C79F2C")]
    public readonly InputSlot<SceneSetup> Setup = new();

    [Input(Guid = "EF7075E9-4BC2-442E-8E0C-E03667FF2E0A")]
    public readonly InputSlot<bool> TriggerUpdate = new();
}