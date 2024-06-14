namespace T3.Core.DataTypes
{
    public class Texture3dWithViews
    {
        public T3.Core.DataTypes.Texture3D Texture;
        public SharpDX.Direct3D11.ShaderResourceView Srv;
        public SharpDX.Direct3D11.UnorderedAccessView Uav;
        public SharpDX.Direct3D11.RenderTargetView Rtv;
    }
}