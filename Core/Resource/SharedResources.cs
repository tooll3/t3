using System;
using System.IO;
using SharpDX.Direct3D11;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Editor.App
{
    /// <summary>
    /// A collection of rendering resource used across the T3 UI
    /// </summary>
    public static class SharedResources
    {
        public static readonly string Directory = Path.Combine(RuntimeAssemblies.CoreDirectory, ResourceManager.ResourcesSubfolder);
        public static readonly IResourcePackage ResourcePackage = new SharedResourceObject();
        
        static SharedResources()
        {
            ResourceManager.AddSharedResourceFolder(ResourcePackage, true);
        }

        public static void Initialize()
        {
            if (ShaderCompiler.Instance == null)
            {
                throw new Exception($"{nameof(ShaderCompiler)}.{nameof(ShaderCompiler.Instance)} not initialized");
            }
            
            const string errorHeader = $"{nameof(SharedResources)} error: ";

            var resourceManager = ResourceManager.Instance();
            var gotFullscreenVertexShader = resourceManager.TryCreateShaderResource(
                 relativePath: @"dx11\fullscreen-texture.hlsl",
                 instance: null,
                 entryPoint: "vsMain",
                 name: "vs-fullscreen-texture",
                 fileChangedAction: null,
                 resource: out _fullScreenVertexShaderResource,
                 reason: out var errorMessage);
            

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Log.Error(errorHeader + errorMessage);
            }

            if (!gotFullscreenVertexShader)
            {
                throw new Exception("Failed to load fullscreen vertex shader");
            }
            
            var gotFullscreenPixelShader = resourceManager.TryCreateShaderResource(
                relativePath: @"dx11\fullscreen-texture.hlsl",
                instance: null,
                entryPoint: "psMain",
                name: "ps-fullscreen-texture",
                fileChangedAction: null,
                resource: out _fullScreenPixelShaderResource,
                reason: out errorMessage);
            

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Log.Error(errorHeader + errorMessage);
            }
            
            if (!gotFullscreenPixelShader)
            {
                throw new Exception("Failed to load fullscreen pixel shader");
            }
            
            ViewWindowRasterizerState = new RasterizerState(ResourceManager.Device, new RasterizerStateDescription
                                                                                                   {
                                                                                                       FillMode = FillMode.Solid, // Wireframe
                                                                                                       CullMode = CullMode.None,
                                                                                                       IsFrontCounterClockwise = true,
                                                                                                       DepthBias = 0,
                                                                                                       DepthBiasClamp = 0,
                                                                                                       SlopeScaledDepthBias = 0,
                                                                                                       IsDepthClipEnabled = false,
                                                                                                       IsScissorEnabled = default,
                                                                                                       IsMultisampleEnabled = false,
                                                                                                       IsAntialiasedLineEnabled = false
                                                                                                   }); 
            

            (uint texId, var tmpId ) = resourceManager.CreateTextureFromFile(@"t3-editor/images/t3-background.png",  null, null);
            ViewWindowDefaultSrvId = tmpId;
            
            (uint texResourceId3, var srvResourceId ) = resourceManager.CreateTextureFromFile(@"t3-editor/images/t3-colorpicker.png", null, null);
            if (ResourceManager.ResourcesById[srvResourceId] is ShaderResourceViewResource srvResource)
            {
                ColorPickerImageSrv = srvResource.ShaderResourceView;
            }
            else
            {
                Log.Warning("Color picker texture not found");
            }
        }
        
        public static RasterizerState ViewWindowRasterizerState;
        public static uint ViewWindowDefaultSrvId;
        public static ShaderResourceView ColorPickerImageSrv;
        private static ShaderResource<VertexShader> _fullScreenVertexShaderResource;
        private static ShaderResource<PixelShader> _fullScreenPixelShaderResource;

        public static ShaderResource<VertexShader> FullScreenVertexShaderResource => _fullScreenVertexShaderResource;

        public static ShaderResource<PixelShader> FullScreenPixelShaderResource => _fullScreenPixelShaderResource;

        private sealed class SharedResourceObject : IResourcePackage
        {
            // ReSharper disable once ReplaceAutoPropertyWithComputedProperty
            public string ResourcesFolder { get; } = Directory;
            public ResourceFileWatcher FileWatcher => null;
            public bool IsReadOnly => true;
        }
    }
}