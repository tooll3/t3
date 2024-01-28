using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Editor.Gui.Graph.Rendering;

namespace T3.Editor.Gui.Windows.RenderExport;

static class TextureReadAccess
{
    public static bool Setup(Texture2D originalTexture, out DataBox dataBox, out DataStream inputStream)
    {
        dataBox = new DataBox();
        inputStream = null;
        
        if (originalTexture == null || originalTexture.IsDisposed)
            return false;
        
        var currentDesc = originalTexture.Description;
        PrepareCpuAccessTextures(currentDesc);
        PrepareResolveShaderResources();

        ConvertTextureToRgba(originalTexture);

        // Copy the original texture to a readable image
        var immediateContext = ResourceManager.Device.ImmediateContext;
        var cpuAccessTexture = _imagesWithCpuAccess[_currentIndex];
        immediateContext.CopyResource(ConversionTexture, cpuAccessTexture);
        immediateContext.UnmapSubresource(cpuAccessTexture, 0);
            
        _currentIndex = (_currentIndex + 1) % NumTextureEntries;

        // Don't return first two samples since buffering is not ready yet
        if (_currentUsageIndex++ < 0)
            return false;

        // Map image resource to get a stream we can read from
        dataBox = immediateContext.MapSubresource(cpuAccessTexture,
                                                  0,
                                                  0,
                                                  MapMode.Read,
                                                  SharpDX.Direct3D11.MapFlags.None,
                                                  out var stream);

        inputStream = stream;
        return true;
    }
    
    // public static void Release()
    // {
    //     // release our resources
    //     ResourceManager.Device.ImmediateContext.UnmapSubresource(cpuAccessTexture, 0);
    // }

    public static void DisposeTextures()
    {
        ConversionTexture?.Dispose();
        
        foreach (var image in _imagesWithCpuAccess)
        {
            image?.Dispose();
        }
        _imagesWithCpuAccess.Clear();
    }
        
    /// <summary>
    /// create several textures with a given format with CPU access to be able to read out the initial texture values
    /// </summary>
    /// <param name="currentDesc"></param>
    private static void PrepareCpuAccessTextures(Texture2DDescription currentDesc)
    {
        if (_imagesWithCpuAccess.Count != 0
            // && _imagesWithCpuAccess[0].Description.Format == currentDesc.Format
            && _imagesWithCpuAccess[0].Description.Width == currentDesc.Width
            && _imagesWithCpuAccess[0].Description.Height == currentDesc.Height
            // && _imagesWithCpuAccess[0].Description.MipLevels == currentDesc.MipLevels
           )
            return;
        
        DisposeTextures();
        
        // Create read back textures
        var cpuAccessDescription = new Texture2DDescription
                                       {
                                           BindFlags = BindFlags.None,
                                           Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                                           Width = currentDesc.Width,
                                           Height = currentDesc.Height,
                                           MipLevels = 1,
                                           SampleDescription = new SampleDescription(1, 0),
                                           Usage = ResourceUsage.Staging,
                                           OptionFlags = ResourceOptionFlags.None,
                                           CpuAccessFlags = CpuAccessFlags.Read,
                                           ArraySize = 1
                                       };

        for (var i = 0; i < NumTextureEntries; ++i)
        {
            _imagesWithCpuAccess.Add(new Texture2D(ResourceManager.Device, cpuAccessDescription));
        }

        // Create format conversion texture
        var convertTextureDescription = new Texture2DDescription
                                            {
                                                BindFlags = BindFlags.UnorderedAccess|BindFlags.RenderTarget|BindFlags.ShaderResource,
                                                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                                                Width = currentDesc.Width,
                                                Height = currentDesc.Height,
                                                MipLevels = 1,
                                                SampleDescription = new SampleDescription(1, 0),
                                                Usage = ResourceUsage.Default,
                                                OptionFlags = ResourceOptionFlags.None,
                                                CpuAccessFlags = CpuAccessFlags.None,
                                                ArraySize = 1
                                            };
        ConversionTexture = new Texture2D(ResourceManager.Device, convertTextureDescription);
        _conversionSrv = SrvManager.GetSrvForTexture(ConversionTexture);
        _conversionUav = new UnorderedAccessView(ResourceManager.Device, ConversionTexture);
        
        // Skip the first two frames since they will only appear after buffers have been swapped.
        _currentIndex = 0;
        _currentUsageIndex = -SkipImages;
    }
    
    #region conversion shader
    
    private static void PrepareResolveShaderResources()
    {
        if (_convertComputeShaderResource != null)
            return;
            
        const string sourcePath = @"Resources\lib\img\ConvertFormat-cs.hlsl ";
        const string entryPoint = "main";
        const string debugName = "resolve-convert-texture-format";
        var resourceManager = ResourceManager.Instance();
            
        var success = resourceManager.TryCreateShaderResource(out _convertComputeShaderResource, 
                                                              fileName: sourcePath, 
                                                              entryPoint: entryPoint, 
                                                              name: debugName,
                                                              errorMessage: out var errorMessage);

        if(!success || !string.IsNullOrWhiteSpace(errorMessage))
            Log.Error($"Failed to initialize video conversion shader: {errorMessage}");
    }   
    
    private static void ConvertTextureToRgba(Texture2D inputTexture)
    {
        var device = ResourceManager.Device;
        var deviceContext = device.ImmediateContext;
        var csStage = deviceContext.ComputeShader;
        
        // Keep previous setup
        var prevShader = csStage.Get();
        var prevUavs = csStage.GetUnorderedAccessViews(0, 1);
        var prevSrvs = csStage.GetShaderResources(0, 1);
    
        var convertShader = _convertComputeShaderResource.Shader;
        csStage.Set(convertShader);
    
        const int threadNumX = 16, threadNumY = 16;
        var srv = SrvManager.GetSrvForTexture(inputTexture);
        csStage.SetShaderResource(0, srv);
        csStage.SetUnorderedAccessView(0, _conversionUav, 0);
        
        var dispatchCountX = (inputTexture.Description.Width / threadNumX) + 1;
        var dispatchCountY = (inputTexture.Description.Height / threadNumY) + 1;
        deviceContext.Dispatch(dispatchCountX, dispatchCountY, 1);
            
        // Restore prev setup
        csStage.SetUnorderedAccessView(0, prevUavs[0]);
        csStage.SetShaderResource(0, prevSrvs[0]);
        csStage.Set(prevShader);
    }
    
    private static ShaderResource<SharpDX.Direct3D11.ComputeShader> _convertComputeShaderResource;
    
    
    #endregion
    
    
    
    // Hold several textures internally to speed up calculations
    public static Texture2D ConversionTexture;
    private static ShaderResourceView _conversionSrv;
    private static UnorderedAccessView _conversionUav;
    
    private const int NumTextureEntries = 3;
    private static readonly List<Texture2D> _imagesWithCpuAccess = new();
    private static int _currentIndex;
    private static int _currentUsageIndex;

    /** Skip a certain number of images at the beginning since the
     * final content will only appear after several buffer flips*/
    public const int SkipImages = 0;
    
}