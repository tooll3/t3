using SharpDX.Direct3D11;

namespace T3.Core.DataTypes
{
    [T3Type()]
    public class RenderTargetReference
    {
        public Texture2D ColorTexture;
        public Texture2D DepthTexture;
    }
}