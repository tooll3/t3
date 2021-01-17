using SharpDX.Direct3D11;

namespace T3.Core.DataTypes
{
    public class PbrMaterialTextures
    {
        public Texture2D AlbedoColorMap;
        public Texture2D EmissiveColorMap;
        public Texture2D RoughnessSpecularMetallicOcclusionMap;
        public Texture2D NormalMap;
    }
}