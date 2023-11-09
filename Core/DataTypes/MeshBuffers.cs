namespace T3.Core.DataTypes
{
    /// <summary>
    /// Combines buffers required for mesh rendering
    /// </summary>
    public class MeshBuffers
    {
        public BufferWithViews VertexBuffer;
        public BufferWithViews IndicesBuffer;

        public override string ToString()
        {
            if(VertexBuffer?.Srv == null && IndicesBuffer?.Srv == null)
                return "Undefined";
            
            var vertexCount = VertexBuffer?.Srv != null 
                                  ? VertexBuffer.Srv.Description.Buffer.ElementCount
                                    :0 ;
            var indicesCount = IndicesBuffer?.Srv != null 
                                  ? IndicesBuffer.Srv.Description.Buffer.ElementCount
                                  :0 ;

            return $"{vertexCount} vertices {indicesCount} faces";
        }
    }
}