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
using T3.Core.Rendering.Material;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;
using Scene = SharpGLTF.Schema2.Scene;

using VERTEXKEY = System.ValueTuple<System.Numerics.Vector3, System.Numerics.Vector3, System.Numerics.Vector2>;

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
            if (child == null)
                continue;

            var t = child.LocalTransform.GetDecomposed();
            var transform = new SceneSetup.Transform
                                {
                                    Translation = t.Translation,
                                    Scale = t.Scale,
                                    Rotation = t.Rotation,
                                };

            // Pure logic node
            var structureNode = new SceneSetup.SceneNode()
                                    {
                                        Name = child.Name,
                                        MeshName = child.Mesh?.Name,
                                        // MeshBuffers = meshBuffers,
                                        Transform = transform,
                                        CombinedTransform = child.WorldMatrix,
                                    };

            parentNode.ChildNodes.Add(structureNode);

            var useStructureNodeForMesh = true;

            if (child.Mesh != null)
            {
                foreach (var meshPrimitive in child.Mesh.Primitives)
                {
                    if (!TryGenerateMeshBuffersFromGltfChild(meshPrimitive, out var meshBuffers, out var errorMessage))
                    {
                        ShowError(errorMessage);
                        meshBuffers = null;
                    }

                    if (meshBuffers == null)
                        continue;


                    var materialDef = GetOrCreateMaterialDefinition(meshPrimitive.Material);
                    
                    Log.Debug("Material: " + materialDef);
                    foreach (var m in meshPrimitive.Material.Channels)
                    {
                        if (m.Key != "BaseColor")
                            continue;
                        
                        
                        Log.Debug("CH: " + m.Color);
                    }
                    
                    if (useStructureNodeForMesh)
                    {
                        structureNode.MeshBuffers = meshBuffers;
                        structureNode.Material = materialDef;
                        useStructureNodeForMesh = false;
                        continue;
                    }

                    var meshNode = new SceneSetup.SceneNode()
                                       {
                                           Name = child.Name,
                                           MeshName = child.Mesh?.Name,
                                           MeshBuffers = meshBuffers,
                                           Transform = transform,
                                           CombinedTransform = child.WorldMatrix,
                                           Material = materialDef,
                                       };
                    parentNode.ChildNodes.Add(meshNode);
                }
            }

            ParseChildren(child.VisualChildren, structureNode);
        }
    }

    /// <summary>
    /// Extract gltf material definitions so we can later create a PbrMaterial from this data.
    /// </summary>
    private SceneSetup.SceneMaterial GetOrCreateMaterialDefinition(Material gltfMaterial)
    {
        if (gltfMaterial == null)
            return null;

        var name = gltfMaterial.Name;
        if (string.IsNullOrEmpty(name))
            return null;

        if (_sceneMaterialsByName.TryGetValue(name, out var materialDef))
        {
            return materialDef;
        }
        
        Vector4 baseColor = default;
        Vector4 emissiveColor = default;
        float roughness = 0.5f;
        float metal = 0;

        foreach (var c in gltfMaterial.Channels)
        {
            switch (c.Key)
            {
                case "BaseColor":
                    baseColor = c.Color;
                    break;
                case "Emissive":
                    emissiveColor = c.Color;
                    break;
                case "MetallicRoughness":
                {
                    foreach (var p in c.Parameters)
                    {
                        switch (p.Name)
                        {
                            case "MetallicFactor":
                                metal = (float)p.Value;
                                break;
                            case "RoughnessFactor":
                                roughness = (float)p.Value;
                                break;
                        }
                    }

                    break;
                }
            }
        }
        
        var newMaterialDef = new SceneSetup.SceneMaterial
                                 {
                                     Name =  name,
                                     PbrParameters = new PbrMaterial.PbrParameters
                                                         {
                                                             BaseColor = baseColor,
                                                             EmissiveColor = emissiveColor,
                                                             Roughness = roughness,
                                                             Specular = 0,
                                                             Metal = metal,
                                                         }
                                 };
        _sceneMaterialsByName[name] = newMaterialDef;
        return newMaterialDef;
    }

    private readonly Dictionary<string, SceneSetup.SceneMaterial> _sceneMaterialsByName = new();
    
    private bool TryGenerateMeshBuffersFromGltfChild(MeshPrimitive meshPrimitive, out MeshBuffers newMesh, out string message)
    {
        // TODO: return cached mesh to reuse buffer
        newMesh = new MeshBuffers();
        message = null;

        var vertexBufferData = Array.Empty<PbrVertex>();
        var indexBufferData = Array.Empty<Int3>();

        try
        {
            // Convert vertices
            var verticesCount = 0;
            {
                // TODO: Iterate over all primitives
                var vertexAccessors = meshPrimitive.VertexAccessors;
                var keys = vertexAccessors.Keys;
                var keys2 = string.Join(",", keys);
                // Log.Debug($"found attributes :{keys2}", this);

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
                                                                                         1-texCoords[vertexIndex].Y),
                                                            Selection = 1,
                                                        };
                }
            }

            // Convert indices
            int faceCount;
            int updatedTangentCount = 0;
            {
                var indices = meshPrimitive.GetTriangleIndices().ToList();
                faceCount = indices.Count;
                if (indexBufferData.Length != faceCount)
                    indexBufferData = new Int3[faceCount];

                var faceIndex = 0;
                foreach (var (a, b, c) in indices)
                {
                    indexBufferData[faceIndex] = new Int3(a, b, c);
                    faceIndex++;
                    
                    // Calc TBN space
                    var p1 = vertexBufferData[a].Position;
                    var p2 = vertexBufferData[b].Position;
                    var p3 = vertexBufferData[c].Position;
                    
                    // check for degenerated triangle
                    if (p1 == p2 || p1 == p3 || p2 == p3) continue;
                    
                    var uv1 = vertexBufferData[a].Texcoord;
                    var uv2 = vertexBufferData[b].Texcoord;
                    var uv3 = vertexBufferData[c].Texcoord;

                    
                    // check for degenerated triangle
                    if (uv1 == uv2 || uv1 == uv3 || uv2 == uv3) continue;
                    
                    var n1 = vertexBufferData[a].Normal;
                    var n2 = vertexBufferData[b].Normal;
                    var n3 = vertexBufferData[c].Normal;
                    
                    
                    // Taken from https://github.com/vpenades/SharpGLTF/blob/master/examples/SharpGLTF.Runtime.MonoGame/NormalTangentFactories.cs
                    var s = p2 - p1;
                    var t = p3 - p1;
                    
                    var sUv = uv2 - uv1;
                    var tUv = uv3 - uv1;
                    //var tUv =  uv1 - uv3;
                    //tUv.Y = 1 - tUv.Y; 
                    
                    var sx = sUv.X;
                    var tx = tUv.X;
                    var sy = sUv.Y;
                    var ty = tUv.Y;
                    
                    var r = 1.0F / ((sx * ty) - (tx * sy));
                    
                    if (!r._IsFinite()) continue;
                    
                    var sDir = new Vector3((ty * s.X) - (sy * t.X), (ty * s.Y) - (sy * t.Y), (ty * s.Z) - (sy * t.Z)) * r;
                    var tDir = new Vector3((sx * t.X) - (tx * s.X), (sx * t.Y) - (tx * s.Y), (sx * t.Z) - (tx * s.Z)) * r;
                    
                    if (!sDir._IsFinite()) continue;
                    if (!tDir._IsFinite()) continue;
                    
                    // Ill-fated attempt with brute force 
                    // sDir =  Vector3.Cross(n1, Vector3.UnitY);
                    // tDir =  Vector3.Cross(n1, sDir);
                    
                    // Todo: Sadly this fill add significant artifacts to complex meshes
                    vertexBufferData[a].Tangent = sDir;
                    vertexBufferData[a].Bitangent = tDir;
                    
                    updatedTangentCount++;
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

            // Log.Debug($"  loaded gltf-child:  {verticesCount} vertices  {faceCount} faces  updated {updatedTangentCount} tangents", this);

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