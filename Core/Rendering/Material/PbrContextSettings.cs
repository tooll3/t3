using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.Operator;

namespace T3.Core.Rendering.Material;

public static class PbrContextSettings
{
    public static void SetDefaultToContext(EvaluationContext context)
    {
        if (!_wasInitialized)
            InitDefaultMaterialAndResources();

        context.Materials.Clear();
        context.PbrMaterial = _defaultMaterial;
        context.ContextTextures[PrefilteredSpecularId] = _prefilteredBrdfTexture;
    }

    private static void InitDefaultMaterialAndResources()
    {
        WhitePixelTexture = TextureUtils.CreateColorTexture(new Vector4(1, 1, 1, 1));
        BlackPixelTexture = TextureUtils.CreateColorTexture(new Vector4(0, 0, 0, 0));

        PbrLookUpTextureSrv = TextureUtils.LoadTextureAsSrv(@"Resources\common\images\BRDF-LookUp.dds");
        _prefilteredBrdfTexture = TextureUtils.LoadTexture(@"Resources\common\HDRI\studio_small_08-prefiltered.dds");

        _defaultMaterial = PbrMaterial.CreateDefault();
        _wasInitialized = true;
    }

    private static PbrMaterial _defaultMaterial;

    public static Texture2D WhitePixelTexture; // TODO: move to something like shared resource
    public static Texture2D BlackPixelTexture; // TODO: move to something like shared resource
    
    public static ShaderResourceView PbrLookUpTextureSrv;
    
    public const string PrefilteredSpecularId = "PrefilteredSpecular";
    private static Texture2D _prefilteredBrdfTexture;

    private static bool _wasInitialized;
}