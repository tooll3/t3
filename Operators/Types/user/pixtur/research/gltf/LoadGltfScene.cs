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

    [Output(Guid = "c830b31a-b0d9-4a12-8883-da7734541e54")]
    public readonly Slot<MeshBuffers> Data = new();

    public LoadGltfScene()
    {
        ResultSetup.UpdateAction = Update;
        Data.UpdateAction = Update;
    }

    private SceneSetup _sceneSetup;

    private void Update(EvaluationContext context)
    {
        _sceneSetup = Setup.GetValue(context);
        if (_sceneSetup == null)
        {
            _sceneSetup = new SceneSetup();
            Setup.SetTypedInputValue(_sceneSetup);
        }

        var path = Path.GetValue(context);
        var childIndexChanged = ChildIndex.DirtyFlag.IsDirty;

        var childIndex = ChildIndex.GetValue(context);

        if (path != _lastFilePath || childIndexChanged)
        {
            
            _lastFilePath = path;

            if (!File.Exists(path))
            {
                ShowError($"Gltf File not found: {path}");
                return;
            }

            var fullPath = System.IO.Path.GetFullPath(path);
            var model = SharpGLTF.Schema2.ModelRoot.Load(fullPath);
            
            // if(childIndexChanged)
            //     UpdateBuffers(model, childIndex);
            
            var rootNode = ParseStructure(model.DefaultScene);
            _sceneSetup.Nodes.Clear();
            _sceneSetup.Nodes.Add(rootNode);

            ResultSetup.Value = _sceneSetup;
        }
        
        
        
    }

    private void ShowError(string message)
    {
        _lastErrorMessage = message;
        Log.Warning(_lastErrorMessage, this);
    }
    
    

    private SceneSetup.SceneNode ParseStructure(Scene modelDefaultScene)
    {
        var rootNode = new SceneSetup.SceneNode()
                           {
                               Name = modelDefaultScene.Name
                           };
        
        _allMeshes.Clear();
        ParseChildren(modelDefaultScene.VisualChildren, rootNode, _allMeshes);

        // var totalFaceCount = 0;
        // var totalIndicesCount = 0;
        
        foreach (var mesh in _allMeshes)
        {
            var vertexCount = 0;
            var faceCount = 0;
            foreach (var p in mesh.Primitives)
            {
                // Collect positions
                if (!p.VertexAccessors.TryGetValue("POSITION", out var positionAccessor) || positionAccessor == null)
                {
                    ShowError("Can't find POSITION attribute in gltf mesh");
                    continue;
                }                
                vertexCount += positionAccessor.Count;
                
                faceCount += p.GetTriangleIndices().Count();

            }
            Log.Debug($"Found {mesh.Name} with {mesh.Primitives.Count} primitives / {vertexCount} vertices / {faceCount} faces");
        }
        return rootNode;
    }

    
    public class MeshFragment
    {
        public int StartTriangleIndex;
        public int TriangleCount;
        public Mesh Mesh;
        public int MeshPrimitiveIndex;
        public int MaterialIndex;
    } 
    
    
    private static void ParseChildren(IEnumerable<Node> visualChildren, SceneSetup.SceneNode rootNode, HashSet<Mesh> allMeshes)
    {
        foreach (var child in visualChildren)
        {
            var newSceneChildNode = new SceneSetup.SceneNode()
                                        {
                                            Name = child.Name,
                                            MeshName = child.Mesh?.Name,
                                        };
            rootNode.ChildNodes.Add(newSceneChildNode);
            if (child.Mesh != null)
            {
                // if (allMeshes.TryGetValue(child.Mesh.Name, out var existingMesh))
                // {
                //     if (existingMesh != child.Mesh)
                //     {
                //         Log.Warning($"Mesh {child.Mesh.Name} already defined with another mesh instance?");
                //         continue;
                //     }
                // }
                allMeshes.Add(child.Mesh);
            }
            
            ParseChildren(child.VisualChildren, newSceneChildNode, allMeshes);
        }
    }


    // private void UpdateBuffers(ModelRoot model, int childIndex)
    // {
    //     var children = model.DefaultScene.VisualChildren.ToList();
    //     if (childIndex < 0 && childIndex >= children.Count)
    //     {
    //         ShowError($"gltf child index {childIndex} exceeds visible children in default scene {children.Count}");
    //         return;
    //     }
    //
    //     var child = children[childIndex];
    //     Data.Value = GenerateMeshDataFromGltfChild(child);
    // }


    private MeshBuffers GenerateMeshDataFromGltfChild(ModelRoot model)
    {
        // foreach (var meshBuffer in model.LogicalBuffers)
        // {
        //     model.LogicalBuffers
        // }
        
        
        //var newData = new GltfDataSet();
        var newMesh = new MeshBuffers();
        
        var vertexBufferData = Array.Empty<PbrVertex>();
        var indexBufferData = Array.Empty<Int3>();

        var child = model.DefaultScene.VisualChildren.ToArray()[0];
        
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
                                                                                                 texCoords[vertexIndex].Y),
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

    private string _lastFilePath;

    [Input(Guid = "292e80cf-ba31-4a50-9bf4-83712430f811")]
    public readonly InputSlot<string> Path = new();

    [Input(Guid = "a82b725b-5a2b-4679-85be-2641c45d3687")]
    public readonly InputSlot<int> ChildIndex = new();

    [Input(Guid = "D02F41A6-1A6B-4A6E-8D6C-A28873C79F2C")]
    public readonly InputSlot<SceneSetup> Setup = new();

    public IStatusProvider.StatusLevel GetStatusLevel()
    {
        return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
    }

    public string GetStatusMessage()
    {
        return _lastErrorMessage;
    }

    private string _lastErrorMessage;
    private HashSet<Mesh> _allMeshes = new();
}