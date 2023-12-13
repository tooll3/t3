using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Editor.App
{
    /// <summary>
    /// A collection of rendering resource used across the T3 UI
    /// </summary>
    public static class SharedResources
    {
        public static void Initialize(ResourceManager resourceManager)
        {
            const string errorHeader = $"{nameof(SharedResources)} error: ";
            
            var gotFullscreenVertexShader = resourceManager.TryCreateShaderResource<VertexShader>(
                 fileName: @"Resources\lib\dx11\fullscreen-texture.hlsl",
                 entryPoint: "vsMain",
                 name: "vs-fullscreen-texture",
                 fileChangedAction: () => { },
                 resource: out FullScreenVertexShaderResource,
                 errorMessage: out var errorMessage);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Log.Error(errorHeader + errorMessage);
            }
            
            var gotFullscreenPixelShader = resourceManager.TryCreateShaderResource<PixelShader>(
                fileName: @"Resources\lib\dx11\fullscreen-texture.hlsl",
                entryPoint: "psMain",
                name: "ps-fullscreen-texture",
                fileChangedAction: () => { },
                resource: out FullScreenPixelShaderResource,
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
            

            (uint texId, var tmpId ) = resourceManager.CreateTextureFromFile(@"Resources\t3-editor\images\t3-background.png", null);
            ViewWindowDefaultSrvId = tmpId;
            
            (uint texResourceId3, var srvResourceId ) = resourceManager.CreateTextureFromFile(@"Resources\t3-editor\images\t3-colorpicker.png", null);
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
        public static ShaderResource<VertexShader> FullScreenVertexShaderResource;
        public static ShaderResource<PixelShader> FullScreenPixelShaderResource;
    }
}