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
        public static readonly string Directory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Resources");
        
        static SharedResources()
        {
            ResourceManager.SharedResourcePackages.Add(new SharedResourceObject());
        }

        public static void Initialize()
        {
            const string errorHeader = $"{nameof(SharedResources)} error: ";

            var resourceManager = ResourceManager.Instance();
            var gotFullscreenVertexShader = resourceManager.TryCreateShaderResource<VertexShader>(
                 relativePath: @"dx11\fullscreen-texture.hlsl",
                 instance: null,
                 entryPoint: "vsMain",
                 name: "vs-fullscreen-texture",
                 fileChangedAction: () => { },
                 resource: out _fullScreenVertexShaderResource,
                 errorMessage: out var errorMessage);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Log.Error(errorHeader + errorMessage);
            }
            
            var gotFullscreenPixelShader = resourceManager.TryCreateShaderResource<PixelShader>(
                relativePath: @"dx11\fullscreen-texture.hlsl",
                instance: null,
                entryPoint: "psMain",
                name: "ps-fullscreen-texture",
                fileChangedAction: () => { },
                resource: out _fullScreenPixelShaderResource,
                errorMessage: out errorMessage);
            

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Log.Error(errorHeader + errorMessage);
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

        private sealed class SharedResourceObject : IResourceContainer
        {
            // ReSharper disable once ReplaceAutoPropertyWithComputedProperty
            public string ResourcesFolder { get; } = Directory;
            public ResourceFileWatcher FileWatcher => null;
        }
    }
}