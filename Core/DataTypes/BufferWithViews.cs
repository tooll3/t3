using SharpDX.Direct3D11;

namespace T3.Core.DataTypes
{
    public class BufferWithViews
    {
        public SharpDX.Direct3D11.Buffer Buffer;
        public SharpDX.Direct3D11.ShaderResourceView Srv;
        public SharpDX.Direct3D11.UnorderedAccessView Uav;
    }
}