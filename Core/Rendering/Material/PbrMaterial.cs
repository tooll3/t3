using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Resource;

namespace T3.Core.Rendering.Material;

/// <summary>
/// Contains all settings and resource views to initialize a draw call with this material
/// </summary>
public class PbrMaterial
{
    public string Name;
    public ShaderResourceView AlbedoMapSrv;
    public ShaderResourceView EmissiveMapSrv;
    public ShaderResourceView RoughnessMetallicOcclusionSrv;
    public ShaderResourceView NormalSrv;

    public PbrParameters Parameters;
    public Buffer ParameterBuffer;

    public void UpdateParameterBuffer()
    {
        ResourceManager.SetupConstBuffer(Parameters, ref ParameterBuffer);
    }

    [StructLayout(LayoutKind.Explicit, Size = Stride)]
    public struct PbrParameters
    {
        [FieldOffset(0)]
        public Vector4 BaseColor;

        [FieldOffset(4 * 4)]
        public Vector4 EmissiveColor;

        [FieldOffset(8 * 4)]
        public float Roughness;

        [FieldOffset(9 * 4)]
        public float Specular;

        [FieldOffset(10 * 4)]
        public float Metal;

        [FieldOffset(11 * 4)]
        private float __padding;

        public const int Stride = 12 * 4;
    }


    public static PbrMaterial CreateDefault()
    {
        _rmoDefaultTexture = TextureUtils.CreateColorTexture(new Vector4(0f, 0, 1, 0));
        _normalDefaultTexture = TextureUtils.CreateColorTexture(new Vector4(0.5f, 0.5f, 1, 0));

        DefaultAlbedoColorSrv = new ShaderResourceView(ResourceManager.Device, PbrContextSettings.WhitePixelTexture);
        DefaultEmissiveColorSrv = new ShaderResourceView(ResourceManager.Device, PbrContextSettings.WhitePixelTexture);
        DefaultRoughnessMetallicOcclusionSrv = new ShaderResourceView(ResourceManager.Device, _rmoDefaultTexture);
        DefaultNormalSrv = new ShaderResourceView(ResourceManager.Device, _normalDefaultTexture);
        
        var newMaterial= new PbrMaterial()
                             {
                                 Name = "Default",
                                 Parameters = DefaultParameters,
                                 AlbedoMapSrv = DefaultAlbedoColorSrv,
                                 EmissiveMapSrv = DefaultEmissiveColorSrv,
                                 RoughnessMetallicOcclusionSrv = DefaultRoughnessMetallicOcclusionSrv,
                                 NormalSrv = DefaultNormalSrv,

                             };
        newMaterial.UpdateParameterBuffer();
        return newMaterial;
    }

    public static PbrParameters DefaultParameters = new()
                                                        {
                                                            BaseColor = Vector4.One,
                                                            EmissiveColor = new Vector4(0, 0, 0, 1),
                                                            Roughness = 0.5f,
                                                            Specular = 10,
                                                            Metal = 0
                                                        };
    public static ShaderResourceView DefaultEmissiveColorSrv;
    public static ShaderResourceView DefaultAlbedoColorSrv;
    public static ShaderResourceView DefaultRoughnessMetallicOcclusionSrv;
    public static ShaderResourceView DefaultNormalSrv;
    
    private static Texture2D _rmoDefaultTexture;
    private static Texture2D _normalDefaultTexture;
}