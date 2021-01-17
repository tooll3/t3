using SharpDX.Direct3D11;

namespace T3.Core.DataTypes
{
    public class PbrMaterialTextures
    {
        public ShaderResourceView AlbedoColorMap;
        public ShaderResourceView EmissiveColorMap;
        public ShaderResourceView RoughnessSpecularMetallicOcclusionMap;
        public ShaderResourceView NormalMap;
    }
}