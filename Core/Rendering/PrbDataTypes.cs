using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = Stride)]
    public struct PbrVertex
    {
        [FieldOffset(0)]
        public SharpDX.Vector3 Position;

        [FieldOffset(3 * 4)]
        public SharpDX.Vector3 Normal;

        [FieldOffset(6 * 4)]
        public SharpDX.Vector3 Tangent;

        [FieldOffset(9 * 4)]
        public SharpDX.Vector3 Bitangent;

        [FieldOffset(12 * 4)]
        public SharpDX.Vector2 Texcoord;

        [FieldOffset(14 * 4)]
        public float Selection; 

        [FieldOffset(15 * 4)]
        private float __padding; 

        public const int Stride = 16 * 4;
    }

    [StructLayout(LayoutKind.Explicit, Size = 4 * 4)]
    public struct PbrFace
    {
        [FieldOffset(0)]
        public SharpDX.Int3 VertexIndices;

        [FieldOffset(3 * 4)]
        private readonly float __padding;
    }

    public static class PbrContextSettings
    {
        public static void SetDefaultToContext(EvaluationContext context)
        {
            if (!_wasInitialized)
                Init();

            context.PbrMaterialParams = _defaultParameterBuffer;
            context.PbrMaterialTextures.AlbedoColorMap = _baseColorMapSrv;
            context.PbrMaterialTextures.NormalMap = _normalMapSrv;
            context.PbrMaterialTextures.RoughnessSpecularMetallicOcclusionMap = _rsmoMapSrv;
            context.PbrMaterialTextures.EmissiveColorMap = _emissiveColorMapSrv;
            context.PbrMaterialTextures.BrdfLookUpMap = _pbrLookUpTexture;
            context.ContextTextures["PrefilteredSpecular"] = _prefilteredBrdfTexture;
        }

        private static void Init()
        {
            var content = new PbrMaterialParams
                              {
                                  BaseColor = Vector4.One,
                                  EmissiveColor = new Vector4(0, 0, 0, 1),
                                  Roughness = 0.5f,
                                  Specular = 10,
                                  Metal = 0
                              };
            ResourceManager.SetupConstBuffer(content, ref _defaultParameterBuffer);

            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;

            WhitePixelTexture = CreateFallBackTexture(new Vector4(1, 1, 1, 1));
            _baseColorMapSrv = new ShaderResourceView(device, WhitePixelTexture);
            _emissiveColorMapSrv = new ShaderResourceView(device, WhitePixelTexture);

            RsmoFallbackTexture = CreateFallBackTexture(new Vector4(0, 1, 0, 0));
            _rsmoMapSrv = new ShaderResourceView(device, RsmoFallbackTexture);

            NormalFallbackTexture = CreateFallBackTexture(new Vector4(0.5f, 0.5f, 1, 0));
            _normalMapSrv = new ShaderResourceView(device, NormalFallbackTexture);
            
            _pbrLookUpTexture = LoadTextureAsSRV(@"Resources\common\images\BRDF-LookUp.dds");
            _prefilteredBrdfTexture = LoadTexture(@"Resources\common\HDRI\studio_small_08-prefiltered.dds");
            
            _wasInitialized = true;
        }

        private static Texture2D CreateFallBackTexture(Vector4 c)
        {
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;

            var colorDesc = new Texture2DDescription()
                                {
                                    ArraySize = 1,
                                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                                    CpuAccessFlags = CpuAccessFlags.None,
                                    Format = Format.R16G16B16A16_Float,
                                    Width = 1,
                                    Height = 1,
                                    MipLevels = 0,
                                    OptionFlags = ResourceOptionFlags.None,
                                    SampleDescription = new SampleDescription(1, 0),
                                    Usage = ResourceUsage.Default
                                };

            var colorBuffer = new Texture2D(device, colorDesc);
            var colorBufferRtv = new RenderTargetView(device, colorBuffer);
            device.ImmediateContext.ClearRenderTargetView(colorBufferRtv, new Color(c.X, c.Y, c.Z, c.W));
            return colorBuffer;
        }

        private static ShaderResourceView LoadTextureAsSRV(string imagePath)
        {
            var resourceManager = ResourceManager.Instance();
            try
            {
                var (textureResId, srvResId) = resourceManager.CreateTextureFromFile(imagePath, () => { });
                
                if (ResourceManager.ResourcesById.TryGetValue(srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
                    return srvResource.ShaderResourceView;                

                Log.Warning($"Failed loading texture {imagePath}");
            }
            catch(Exception e)
            {
                Log.Warning($"Failed loading texture {imagePath} " + e );
            }
            return null;
        }

        private static Texture2D LoadTexture(string imagePath)
        {
            var resourceManager = ResourceManager.Instance();
            try
            {
                var (textureResId, srvResId) = resourceManager.CreateTextureFromFile(imagePath, () => { });
                if (ResourceManager.ResourcesById.TryGetValue(textureResId, out var resource1) && resource1 is Texture2dResource textureResource)
                     return textureResource.Texture;
                
                Log.Warning($"Failed loading texture {imagePath}");
            }
            catch(Exception e)
            {
                Log.Warning($"Failed loading texture {imagePath} " + e );
            }
            return null;
        }

        
        private static ShaderResourceView _baseColorMapSrv;
        private static ShaderResourceView _rsmoMapSrv;
        private static ShaderResourceView _normalMapSrv;
        private static ShaderResourceView _emissiveColorMapSrv;
        private static ShaderResourceView _pbrLookUpTexture;
        private static Texture2D _prefilteredBrdfTexture;
        private static Buffer _defaultParameterBuffer = null;
        
        public static Texture2D WhitePixelTexture;
        public static Texture2D RsmoFallbackTexture;
        public static Texture2D NormalFallbackTexture;
        private static bool _wasInitialized;
    }

    public class PbrMaterialTextures
    {
        public ShaderResourceView AlbedoColorMap;
        public ShaderResourceView EmissiveColorMap;
        public ShaderResourceView RoughnessSpecularMetallicOcclusionMap;
        public ShaderResourceView NormalMap;
        public ShaderResourceView BrdfLookUpMap;
    }

    [StructLayout(LayoutKind.Explicit, Size = PbrMaterialParams.Stride)]
    public struct PbrMaterialParams
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
}