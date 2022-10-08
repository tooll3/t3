using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;

namespace T3.App
{
    /// <summary>
    /// A collection of rendering resource used across the T3 UI
    /// </summary>
    public static class SharedResources
    {
        public static void Initialize(ResourceManager resourceManager)
        {
            FullScreenVertexShaderId =
                resourceManager.CreateVertexShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "vsMain", "vs-fullscreen-texture", () => { });
            FullScreenPixelShaderId =
                resourceManager.CreatePixelShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "psMain", "ps-fullscreen-texture", () => { });
            
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
        
        public static uint FullScreenVertexShaderId;
        public static uint FullScreenPixelShaderId;
        public static RasterizerState ViewWindowRasterizerState;
        public static uint ViewWindowDefaultSrvId;
        public static ShaderResourceView ColorPickerImageSrv;
    }
}