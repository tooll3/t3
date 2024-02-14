using System.Collections.Generic;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Editor.Gui.Graph.Rendering;

namespace T3.Editor.Gui.Windows.RenderExport;

internal static class TextureReadAccess
{
    public class ReadRequestItem
    {
        public int RequestIndex;
        public string Filepath;
        public ScreenshotWriter.FileFormats FileFormat;
        public Texture2D CpuAccessTexture;
        public OnReadComplete OnSuccess;

        public bool IsReady => RequestIndex == _swapCounter - (CpuAccessTextureCount - 2);
        public bool IsObsolete => RequestIndex < _swapCounter - (CpuAccessTextureCount - 2);
    }

    /// <summary>
    /// Saving a screenshot will take several frames because it takes a while until the frames are
    /// downloaded from the gpu. The method need to be called once a frame.
    /// </summary>
    public static void Update()
    {
        _swapCounter++;

        // Clear obsolete items
        while (_readRequests.Count > 0 && _readRequests[0].IsObsolete)
        {
            Log.Debug("Remove obsolete");
            _readRequests.RemoveAt(0);
        }
        
        if (_readRequests.Count == 0 || !_readRequests[0].IsReady)
            return;

        var completedRequest = _readRequests[0];
        //Log.Debug($"Completed frame i{completedRequest.RequestIndex} Run:{completedRequest.RequestRunTime:0.000}s Play:{completedRequest.RequestPlayback:0.000}s   completed {Playback.RunTimeInSecs:0.000}");
        completedRequest.OnSuccess(completedRequest);
        _readRequests.RemoveAt(0);
    }

    public delegate void OnReadComplete(ReadRequestItem cpuAccessTexture);

    /// <summary>
    /// Convert into BRGA and initiate the readback process.
    /// It will take several frames, until the texture is accessible and the callback is called.
    /// </summary>
    public static bool InitiateRead(Texture2D originalTexture, OnReadComplete onSuccess, string filepath=null)
    {
        // dataBox = new DataBox();
        // inputStream = null;
        //cpuAccessTexture = null;

        if (originalTexture == null || originalTexture.IsDisposed)
            return false;

        PrepareCpuAccessTextures(originalTexture.Description);
        PrepareResolveShaderResources();

        ConvertTextureToRgba(originalTexture);

        // Copy the original texture to a readable image
        var immediateContext = ResourceManager.Device.ImmediateContext;
        var cpuAccessTexture = _imagesWithCpuAccess[_swapCounter % CpuAccessTextureCount];
        immediateContext.CopyResource(_conversionTexture, cpuAccessTexture);
        immediateContext.UnmapSubresource(cpuAccessTexture, 0);

        _readRequests.Add(new ReadRequestItem
                              {
                                  RequestIndex = _swapCounter,
                                  Filepath = filepath,
                                  FileFormat = ScreenshotWriter.FileFormats.Png, // FIXME: this is weird.
                                  CpuAccessTexture = cpuAccessTexture,
                                  OnSuccess = onSuccess,
                              });

        // _currentIndex = (_currentIndex + 1) % CpuAccessTextureCount;
        //
        // // Don't return first two samples since buffering is not ready yet
        // if (_currentUsageIndex++ < 0)
        //     return false;

        return true;
    }

    // public static void Release()
    // {
    //     // release our resources
    //     ResourceManager.Device.ImmediateContext.UnmapSubresource(cpuAccessTexture, 0);
    // }

    public static void DisposeTextures()
    {
        _conversionTexture?.Dispose();

        foreach (var image in _imagesWithCpuAccess)
        {
            image?.Dispose();
        }

        _imagesWithCpuAccess.Clear();
    }

    /// <summary>
    /// Create several textures with a given format with CPU access to be able to read out the initial texture values
    /// </summary>
    private static void PrepareCpuAccessTextures(Texture2DDescription currentDesc)
    {
        if (_imagesWithCpuAccess.Count != 0
            && _imagesWithCpuAccess[0].Description.Width == currentDesc.Width
            && _imagesWithCpuAccess[0].Description.Height == currentDesc.Height
           )
            return;

        DisposeTextures();
        if (_readRequests.Count > 0)
        {
            Log.Debug($"Discarding {_readRequests.Count} texture frames with outdated format");
            _readRequests.Clear();
        }

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

        for (var i = 0; i < CpuAccessTextureCount; ++i)
        {
            _imagesWithCpuAccess.Add(new Texture2D(ResourceManager.Device, cpuAccessDescription));
        }

        // Create format conversion texture
        var convertTextureDescription = new Texture2DDescription
                                            {
                                                BindFlags = BindFlags.UnorderedAccess | BindFlags.RenderTarget | BindFlags.ShaderResource,
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
        _conversionTexture = new Texture2D(ResourceManager.Device, convertTextureDescription);
        _conversionUav = new UnorderedAccessView(ResourceManager.Device, _conversionTexture);
    }

    #region conversion shader
    private static void PrepareResolveShaderResources()
    {
        if (_convertComputeShaderResource != null)
            return;

        const string sourcePath = @"Resources\lib\img\ConvertFormat-cs.hlsl";
        const string entryPoint = "main";
        const string debugName = "resolve-convert-texture-format";
        var resourceManager = ResourceManager.Instance();

        var success = resourceManager.TryCreateShaderResource(out _convertComputeShaderResource,
                                                              fileName: sourcePath,
                                                              entryPoint: entryPoint,
                                                              name: debugName,
                                                              errorMessage: out var errorMessage);

        if (!success || !string.IsNullOrWhiteSpace(errorMessage))
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

    private static readonly List<ReadRequestItem> _readRequests = new(3);

    private static int _swapCounter;

    private static Texture2D _conversionTexture;
    private static UnorderedAccessView _conversionUav;
    
    private const int CpuAccessTextureCount = 3;
    private static readonly List<Texture2D> _imagesWithCpuAccess = new(CpuAccessTextureCount);

    /** Skip a certain number of images at the beginning since the
     * final content will only appear after several buffer flips*/
    public const int SkipImages = 0;

    public static void ClearQueue()
    {
        _readRequests.Clear();
    }
}