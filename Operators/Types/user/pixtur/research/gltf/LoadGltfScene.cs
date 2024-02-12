using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
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

namespace T3.Operators.Types.Id_00618c91_f39a_44ea_b9d8_175c996460dc;

/// <summary>
/// Uses the GltfSharp library to load a gltf file and convert it into a <see cref="SceneSetup"/>.
/// </summary>
public class LoadGltfScene : Instance<LoadGltfScene>
                           , IDescriptiveFilename, IStatusProvider
{
    [Output(Guid = "C2D28EBE-E235-4234-8234-EB56E87ED9AF")]
    public readonly Slot<SceneSetup> ResultSetup = new();

    [Output(Guid = "9891EA8D-7DE7-4DD1-A61E-337ADC569EF3")]
    public readonly Slot<MeshBuffers> Mesh = new();

    [Output(Guid = "F33EC4C1-F07B-46B3-BEC7-34E91B8312EF")]
    public readonly Slot<PbrMaterial> Material = new();

    public LoadGltfScene()
    {
        ResultSetup.UpdateAction = Update;
        Mesh.UpdateAction = Update;
    }

    private void Update(EvaluationContext context)
    {
        _lastErrorMessage = null;

        var materialNeedsUpdate = OffsetRoughness.DirtyFlag.IsDirty || OffsetMetallic.DirtyFlag.IsDirty;

        _offsetRoughness = OffsetRoughness.GetValue(context);
        _offsetMetallic = OffsetMetallic.GetValue(context);

        var meshChildIndex = MeshChildIndex.GetValue(context);

        var sceneSetup = Setup.GetValue(context);
        if (sceneSetup == null || Setup.Input.IsDefault)
        {
            sceneSetup = new SceneSetup();
            Setup.SetTypedInputValue(sceneSetup);
        }

        var filePath = Path.GetValue(context);

        _updateTriggered = TriggerUpdate.GetValue(context);
        TriggerUpdate.SetTypedInputValue(false);

        if (LoadFileIfRequired(filePath, out var newSetup))
        {
            ResultSetup.Value?.Dispose();
            Setup.SetTypedInputValue(newSetup); //TODO: this is weird. 
            ResultSetup.Value = newSetup;
        }

        if (ResultSetup?.Value?.Dispatches != null && ResultSetup.Value.Dispatches.Count > 0)
        {
            var dispatchCount = ResultSetup.Value.Dispatches.Count;
            var index = meshChildIndex.Mod(dispatchCount);
            Mesh.Value = ResultSetup.Value.Dispatches[index].MeshBuffers;
            Material.Value = ResultSetup.Value.Dispatches[index].Material;
        }

        if (materialNeedsUpdate && ResultSetup?.Value?.Dispatches != null && ResultSetup.Value.Dispatches.Count > 0)
        {
            foreach (var dispatch in ResultSetup.Value.Dispatches)
            {
                if (dispatch.Material == null)
                    continue;

                dispatch.Material.Parameters.Roughness = _offsetRoughness;
                dispatch.Material.Parameters.Metal = _offsetMetallic;
                dispatch.Material.UpdateParameterBuffer();
            }
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        if (!isDisposing)
            return;

        ResultSetup.Value?.Dispose();

        Log.Debug("Destroying LoadGltfScene");
    }

    private bool LoadFileIfRequired(string path, out SceneSetup sceneSetup)
    {
        sceneSetup = null;

        if (!_updateTriggered && path == _lastFilePath)
            return false;

        _lastFilePath = path;

        _sceneMaterialsByName.Clear();
        _meshBuffersForPrimitives.Clear();
        sceneSetup = new SceneSetup();

        if (!File.Exists(path))
        {
            ShowError($"Gltf File not found: {path}");
            return true;
        }

        var fullPath = System.IO.Path.GetFullPath(path);
        try
        {
            var model = ModelRoot.Load(fullPath);
            var rootNode = ConvertToNodeStructure(model.DefaultScene);

            sceneSetup.RootNodes.Clear();
            sceneSetup.RootNodes.Add(rootNode);
            sceneSetup.GenerateSceneDrawDispatches();
        }
        catch (Exception e)
        {
            ShowError($"Failed to load gltf file: {path} \n{e.Message}");
            return false;
        }

        return true;
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
                var meshIndex = 0;
                foreach (var meshPrimitive in child.Mesh.Primitives)
                {
                    meshIndex++;
                    if (!TryGenerateMeshBuffersFromGltfChild(meshPrimitive, out var meshBuffers, out var errorMessage))
                    {
                        ShowError(errorMessage);
                        meshBuffers = null;
                    }

                    if (meshBuffers == null)
                        continue;

                    Log.Debug($" mesh:{child.Name} {child?.Mesh?.Name}  {meshIndex}");
                    _meshBuffersForPrimitives[meshPrimitive] = meshBuffers;

                    SceneSetup.SceneMaterial materialDef = GetOrCreateMaterialDefinition(meshPrimitive.Material);

                    //Log.Debug("Material: " + materialDef);

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

    #region Asset extraction
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

        var baseColor = new Vector4(1, 1, 1, 1);
        var baseColorSrv = PbrMaterial.DefaultAlbedoColorSrv;

        var emissiveColor = Vector4.Zero;
        var emissiveSrv = PbrMaterial.DefaultEmissiveColorSrv;

        var normalSrv = PbrMaterial.DefaultNormalSrv;

        ShaderResourceView occlusionSrv = null;
        ShaderResourceView metallicRoughnessSrv = null;
        Texture2D metallicRoughnessTexture = null;
        Texture2D occlusionTexture = null;

        var roughness = 0.5f;
        var metallic = 0f;

        foreach (var channel in gltfMaterial.Channels)
        {
            Log.Debug("channel: " + channel.Key);
            switch (channel.Key)
            {
                case "BaseColor":
                {
                    baseColor = channel.Color;

                    if (TryCreateTextureFromChannel(gltfMaterial, channel, out _, out var srv, PbrMaterial.DefaultAlbedoColorSrv))
                    {
                        baseColorSrv = srv;
                    }

                    break;
                }

                case "Emissive":
                {
                    emissiveColor = channel.Color;

                    if (TryCreateTextureFromChannel(gltfMaterial, channel, out _, out var srv, PbrMaterial.DefaultEmissiveColorSrv))
                    {
                        emissiveSrv = srv;
                    }

                    break;
                }

                case "MetallicRoughness":
                {
                    if (TryCreateTextureFromChannel(gltfMaterial, channel, out var texture, out var srv, null))
                    {
                        metallicRoughnessTexture = texture;
                        metallicRoughnessSrv = srv;
                    }

                    foreach (var p in channel.Parameters)
                    {
                        switch (p.Name)
                        {
                            case "MetallicFactor":
                                metallic = (float)p.Value;
                                break;
                            case "RoughnessFactor":
                                roughness = (float)p.Value;
                                break;
                        }
                    }

                    break;
                }

                case "Occlusion":
                {
                    if (TryCreateTextureFromChannel(gltfMaterial, channel, out var texture, out var srv, null))
                    {
                        occlusionTexture = texture;
                        occlusionSrv = srv;
                    }

                    break;
                }

                case "Normal":
                {
                    if (TryCreateTextureFromChannel(gltfMaterial, channel, out _, out var srv, PbrMaterial.DefaultNormalSrv))
                    {
                        normalSrv = srv;
                    }

                    break;
                }
            }
        }

        TryCreateRoughnessMetallicOcclusionTexture(
                                                   metallicRoughnessTexture, metallicRoughnessSrv,
                                                   occlusionTexture, occlusionSrv,
                                                   out var roughnessMetallicOcclusionSrv);

        metallicRoughnessTexture?.Dispose();
        metallicRoughnessSrv?.Dispose();
        occlusionTexture?.Dispose();
        occlusionSrv?.Dispose();

        var newMaterialDef = new SceneSetup.SceneMaterial
                                 {
                                     Name = name,
                                     // ColorTexture = baseColorTexture,
                                     // NormalTexture = normalTexture,
                                     PbrParameters = new PbrMaterial.PbrParameters
                                                         {
                                                             BaseColor = baseColor,
                                                             EmissiveColor = emissiveColor,
                                                             Roughness = roughness - 1 + _offsetRoughness, // glTF standard uses roughness factor?!
                                                             Specular = 1,
                                                             Metal = metallic -1 + _offsetMetallic, // glTF standard uses metallic factor?!
                                                         }
                                 };

        var newPbrMaterial = new PbrMaterial
                                 {
                                     Name = name,
                                     AlbedoMapSrv = baseColorSrv,
                                     EmissiveMapSrv = emissiveSrv,
                                     RoughnessMetallicOcclusionSrv = roughnessMetallicOcclusionSrv,
                                     NormalSrv = normalSrv,
                                     Parameters = newMaterialDef.PbrParameters,
                                 };
        newPbrMaterial.UpdateParameterBuffer();

        newMaterialDef.PbrMaterial = newPbrMaterial;

        _sceneMaterialsByName[name] = newMaterialDef;
        return newMaterialDef;
    }

    /// <summary>
    /// Setup shaders and textures required for combining roughness, metallic and occlusion textures.
    /// </summary>
    private static void PrepareCombineShaderResources(bool forceUpdate = false)
    {
        if (!forceUpdate && _combineChannelsComputeShaderResource != null)
            return;

        const string sourcePath = @"Resources\lib\cs\CombineGltfChannels-cs.hlsl";
        const string entryPoint = "main";
        const string debugName = "combine-channel-textures";
        var resourceManager = ResourceManager.Instance();

        var success = resourceManager.TryCreateShaderResource(out _combineChannelsComputeShaderResource,
                                                              fileName: sourcePath,
                                                              entryPoint: entryPoint,
                                                              name: debugName,
                                                              errorMessage: out var errorMessage);

        if (!success || !string.IsNullOrWhiteSpace(errorMessage))
            Log.Error($"Failed to initialize video conversion shader: {errorMessage}");

        var samplerDesc = new SamplerStateDescription()
                              {
                                  Filter = Filter.MinMagMipLinear,
                                  AddressU = TextureAddressMode.Clamp,
                                  AddressV = TextureAddressMode.Clamp,
                                  AddressW = TextureAddressMode.Clamp,
                                  MipLodBias = 0,
                                  MaximumAnisotropy = 1,
                                  ComparisonFunction = Comparison.Never,
                                  MinimumLod = -999999,
                                  MaximumLod = 9999999
                              };

        _combineChannelsSampler = new SamplerState(ResourceManager.Device, samplerDesc);
    }

    /// <summary>
    /// Fall back to default texture if nothing set. Otherwise use shader to create new texture.
    /// </summary>
    private void TryCreateRoughnessMetallicOcclusionTexture(Texture2D metallicRoughnessTexture,
                                                            ShaderResourceView metallicRoughnessSrv,
                                                            Texture2D occlusionTexture,
                                                            ShaderResourceView occlusionSrv,
                                                            out ShaderResourceView resultSrv)
    {
        if (metallicRoughnessSrv == null && occlusionSrv == null)
        {
            resultSrv = PbrMaterial.DefaultRoughnessMetallicOcclusionSrv;
            return;
        }

        PrepareCombineShaderResources(_updateTriggered);

        var device = ResourceManager.Device;
        var deviceContext = device.ImmediateContext;
        var csStage = deviceContext.ComputeShader;

        // Compute max resolution
        var width = 1;
        var height = 1;

        if (metallicRoughnessTexture != null)
        {
            width = Math.Max(width, metallicRoughnessTexture.Description.Width);
            height = Math.Max(height, metallicRoughnessTexture.Description.Width);
        }

        if (occlusionTexture != null)
        {
            width = Math.Max(width, occlusionTexture.Description.Width);
            height = Math.Max(height, occlusionTexture.Description.Width);
        }

        metallicRoughnessSrv ??= PbrMaterial.BlackPixelSrv;
        occlusionSrv ??= PbrMaterial.WhitePixelSrv;

        // TODO: create and test merge compute shader

        // Keep previous setup
        var prevShader = csStage.Get();
        var prevUavs = csStage.GetUnorderedAccessViews(0, 1);
        var prevSrvs = csStage.GetShaderResources(0, 1);
        var prevSamplers = csStage.GetSamplers(0, 1);

        // Set Shader
        var convertShader = _combineChannelsComputeShaderResource.Shader;
        csStage.Set(convertShader);

        var srvs = new[] { metallicRoughnessSrv, occlusionSrv };
        csStage.SetShaderResources(0, srvs);

        csStage.SetSampler(0, _combineChannelsSampler);

        // Create target texture
        var resultTextureDescription = new Texture2DDescription
                                           {
                                               BindFlags = BindFlags.UnorderedAccess | BindFlags.RenderTarget | BindFlags.ShaderResource,
                                               Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                                               Width = width,
                                               Height = height,
                                               MipLevels = 1,
                                               SampleDescription = new SampleDescription(1, 0),
                                               Usage = ResourceUsage.Default,
                                               OptionFlags = ResourceOptionFlags.None | ResourceOptionFlags.GenerateMipMaps,
                                               CpuAccessFlags = CpuAccessFlags.None,
                                               ArraySize = 1
                                           };

        var resultTexture = new Texture2D(ResourceManager.Device, resultTextureDescription);
        resultSrv = new ShaderResourceView(ResourceManager.Device, resultTexture);
        var resultUav = new UnorderedAccessView(ResourceManager.Device, resultTexture);
        csStage.SetUnorderedAccessView(0, resultUav, 0);

        // Dispatch
        const int threadNumX = 16, threadNumY = 16;
        var dispatchCountX = (width / threadNumX) + 1;
        var dispatchCountY = (height / threadNumY) + 1;
        deviceContext.Dispatch(dispatchCountX, dispatchCountY, 1);

        ResourceManager.Device.ImmediateContext.GenerateMips(resultSrv);

        // Restore prev setup
        csStage.SetUnorderedAccessView(0, prevUavs[0]);
        csStage.SetShaderResource(0, prevSrvs[0]);
        csStage.SetSamplers(0, prevSamplers);
        csStage.Set(prevShader);
    }

    private static ShaderResource<SharpDX.Direct3D11.ComputeShader> _combineChannelsComputeShaderResource;

    /// <summary>
    /// Tries to create a texture from a gltf material channel.
    /// </summary>
    private static bool TryCreateTextureFromChannel(Material gltfMaterial, MaterialChannel channel, out Texture2D texture, out ShaderResourceView srv,
                                                    ShaderResourceView fallbackSrv)
    {
        texture = null;
        srv = fallbackSrv;
        if (channel.Texture == null)
        {
            return false;
        }

        var imageContent = channel.Texture.PrimaryImage.Content.Content;

        try
        {
            using var memStream = new MemoryStream(imageContent.ToArray());
            memStream.Position = 0;
            using var imagingFactory = new ImagingFactory();

            using var bitmapDecoder = new BitmapDecoder(imagingFactory, memStream, DecodeOptions.CacheOnDemand);
            using var formatConverter = new FormatConverter(imagingFactory);
            using var bitmapFrameDecode = bitmapDecoder.GetFrame(0);
            formatConverter.Initialize(bitmapFrameDecode, SharpDX.WIC.PixelFormat.Format32bppRGBA, BitmapDitherType.None, null, 0.0,
                                       BitmapPaletteType.Custom);

            texture = ResourceManager.CreateTexture2DFromBitmap(ResourceManager.Device, formatConverter);
            texture.DebugName = channel.Key;

            Log.Debug($" Created {gltfMaterial.Name}.{channel.Key} with {texture.Description.Width}×{texture.Description.Height}");
            // bitmapFrameDecode.Dispose();
            // bitmapDecoder.Dispose();
            // formatConverter.Dispose();
            // imagingFactory.Dispose();

            srv = new ShaderResourceView(ResourceManager.Device, texture);
            if (srv == null)
            {
                return false;
            }

            if (srv != null)
                ResourceManager.Device.ImmediateContext.GenerateMips(srv);

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to create texture from channel {gltfMaterial.Name}.{channel.Key} : {e.Message}");
            return false;
        }
    }

    private static bool TryGenerateMeshBuffersFromGltfChild(MeshPrimitive meshPrimitive, out MeshBuffers newMesh, out string message)
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
                                                                                         1 - texCoords[vertexIndex].Y),
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

                    vertexBufferData[a].Tangent = Vector3.Normalize(sDir);
                    vertexBufferData[a].Bitangent = Vector3.Normalize(tDir);

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
    #endregion

    private readonly Dictionary<string, SceneSetup.SceneMaterial> _sceneMaterialsByName = new();
    private readonly Dictionary<MeshPrimitive, MeshBuffers> _meshBuffersForPrimitives = new();

    private float _offsetRoughness;
    private float _offsetMetallic;
    private static SamplerState _combineChannelsSampler;
    private bool _updateTriggered;

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

    private void ShowError(string message)
    {
        _lastErrorMessage = message;
        Log.Warning(_lastErrorMessage, this);
    }

    private string _lastErrorMessage;
    #endregion

    [Input(Guid = "292e80cf-ba31-4a50-9bf4-83712430f811")]
    public readonly InputSlot<string> Path = new();

    [Input(Guid = "D02F41A6-1A6B-4A6E-8D6C-A28873C79F2C")]
    public readonly InputSlot<SceneSetup> Setup = new();

    [Input(Guid = "EF7075E9-4BC2-442E-8E0C-E03667FF2E0A")]
    public readonly InputSlot<bool> TriggerUpdate = new();

    [Input(Guid = "D7AE3173-C490-47CF-8D4B-4C25102F6904")]
    public readonly InputSlot<float> OffsetRoughness = new();

    [Input(Guid = "49237499-9B1E-4371-AFAC-4E3394868370")]
    public readonly InputSlot<float> OffsetMetallic = new();

    [Input(Guid = "FB325383-754A-4702-AFF2-C19E16363460")]
    public readonly InputSlot<int> MeshChildIndex = new();
}