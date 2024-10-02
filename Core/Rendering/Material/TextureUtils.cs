using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;
using T3.Core.Resource;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core.Rendering.Material;

public static class TextureUtils
{
    public static Texture2D CreateColorTexture(Vector4 c)
    {
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

        var colorBuffer = new Texture2D(ResourceManager.Device, colorDesc);
        var colorBufferRtv = new RenderTargetView(ResourceManager.Device, colorBuffer);
        ResourceManager.Device.ImmediateContext.ClearRenderTargetView(colorBufferRtv, new Color(c.X, c.Y, c.Z, c.W));
        return colorBuffer;
    }

    public static ShaderResourceView LoadTextureAsSrv(string imagePath)
    {
        var resourceManager = ResourceManager.Instance();
        try
        {
            var (textureResId, srvResId) = resourceManager.CreateTextureFromFile(imagePath, () => { });

            if (ResourceManager.ResourcesById.TryGetValue(srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
                return srvResource.ShaderResourceView;

            Log.Warning($"Failed loading texture {imagePath}");
        }
        catch (Exception e)
        {
            Log.Warning($"Failed loading texture {imagePath} " + e);
        }

        return null;
    }

    public static Texture2D LoadTexture(string imagePath)
    {
        var resourceManager = ResourceManager.Instance();
        try
        {
            var (textureResId, srvResId) = resourceManager.CreateTextureFromFile(imagePath, () => { });
            if (ResourceManager.ResourcesById.TryGetValue(textureResId, out var resource1) && resource1 is Texture2dResource textureResource)
                return textureResource.Texture;

            Log.Warning($"Failed loading texture {imagePath}");
        }
        catch (Exception e)
        {
            Log.Warning($"Failed loading texture {imagePath} " + e);
        }

        return null;
    }
}