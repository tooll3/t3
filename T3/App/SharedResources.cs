using SharpDX.Direct3D11;
using T3.Core;

namespace t3.App
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
            
            ViewWindowRasterizerState = new RasterizerState(ResourceManager.Instance().Device, new RasterizerStateDescription
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
            

            (uint texId, var tmpId ) = resourceManager.CreateTextureFromFile(@"Resources\images\chipmunk.jpg", null);
            ViewWindowDefaultSrvId = tmpId;
        }
        
        public static uint FullScreenVertexShaderId;
        public static uint FullScreenPixelShaderId;
        public static RasterizerState ViewWindowRasterizerState;
        public static uint ViewWindowDefaultSrvId;
    }
}