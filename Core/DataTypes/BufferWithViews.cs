using System;

namespace T3.Core.DataTypes
{
    public class BufferWithViews : IDisposable
    {
        public SharpDX.Direct3D11.Buffer Buffer;
        public SharpDX.Direct3D11.ShaderResourceView Srv;
        public SharpDX.Direct3D11.UnorderedAccessView Uav;

        
        public void Dispose()
        {
            Buffer?.Dispose();
            Buffer = null;
            
            Srv?.Dispose();
            Srv = null;
            
            Uav?.Dispose();
            Uav = null;
        }
    }
}