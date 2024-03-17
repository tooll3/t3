using System;
using System.Numerics;
using System.Resources;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using ResourceManager = T3.Core.Resource.ResourceManager;

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

        if (!ResourceManager.TryResolvePath("images/BRDF-LookUp.dds", null, out var bdrfPath, out _))
        {
            throw new Exception("Could not find BRDF texture");
        }

        if (!ResourceManager.TryResolvePath("HDRI/studio_small_08-prefiltered.dds", null, out var prefilteredPath, out _))
        {
            throw new Exception("Could not find prefiltered BRDF texture");
        }
        
        PbrLookUpTextureSrv = TextureUtils.LoadTextureAsSrv(bdrfPath);
        _prefilteredBrdfTexture = TextureUtils.LoadTexture(prefilteredPath);

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