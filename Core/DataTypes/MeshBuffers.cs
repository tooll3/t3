using SharpDX.Direct3D11;

namespace T3.Core.DataTypes
{
    /// <summary>
    /// Combines buffers required for mesh rendering
    /// </summary>
    [T3Type()]
    public class MeshBuffers
    {
        public BufferWithViews VertexBuffer;
        public BufferWithViews IndicesBuffer;
    }
}