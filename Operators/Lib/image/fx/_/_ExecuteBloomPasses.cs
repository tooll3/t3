using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using T3.Core.Rendering;
using T3.Core.Utils;
using Utilities = T3.Core.Utils.Utilities;

// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming

namespace Lib.image.fx.@_;

[Guid("9d42dbe7-34a5-4165-877d-6f9c1c675b60")]
internal sealed class _ExecuteBloomPasses : Instance<_ExecuteBloomPasses>
{
    [Output(Guid = "300c319d-86e8-47ce-9597-e81c5a008c8f")]
    public readonly Slot<Texture2D> OutputTexture = new();

    public _ExecuteBloomPasses()
    {
        OutputTexture.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        try
        {
            UpdateSave(context);
        }
        catch (Exception ex)
        {
            Log.Warning(" Failed to execute bloom " + ex.Message, this);
        }
    }

    private void UpdateSave(EvaluationContext context)
    {
        var device = ResourceManager.Device;
        var deviceContext = device.ImmediateContext;

        // Get Inputs
        var sourceTexture = SourceTexture.GetValue(context);
        var vs = FullscreenVS.GetValue(context);
        // ... (get all other shader/sampler inputs) ...
        var brightPassPS = BrightPassPS.GetValue(context);
        var downsamplePS = DownsamplePS.GetValue(context);
        var blurPS = SeparableBlurPS.GetValue(context);
        var upsampleAddPS = UpsampleAddPS.GetValue(context);
        var copyPS = CopyPS.GetValue(context);
        var pointSampler = PointSampler.GetValue(context);
        var linearSampler = LinearSampler.GetValue(context);
        var sourceSrv = SourceTextureSrv.GetValue(context);

        var threshold = Threshold.GetValue(context);
        var intensity = Intensity.GetValue(context); // Overall Intensity 'b'
        var blurOffset = BlurOffset.GetValue(context);
        var levels = Levels.GetValue(context).Clamp(1, 10);
        var shape = Shape.GetValue(context); // Shape 's'
        var clamp = Clamp.GetValue(context);

        // --- Validation ---
        if (sourceTexture == null
            || sourceTexture.IsDisposed
            || sourceSrv == null
            || sourceSrv.IsDisposed
            /* ... other checks ... */
            || vs == null
            || brightPassPS == null
            || downsamplePS == null
            || blurPS == null
            || upsampleAddPS == null
            || copyPS == null
            || pointSampler == null
            || linearSampler == null
           )
        {
            Log.Warning("BloomEffect requires valid inputs.", this);
            OutputTexture.Value = sourceTexture;
            return;
        }

        var initialResolution = new Size2(sourceTexture.Description.Width,
                                          sourceTexture.Description.Height
                                         );
        var initialFormat = sourceTexture.Description.Format;

        // --- Resource Management ---
        // This handles texture creation/recreation based on res/format/levels
        bool resourcesReady = InitializeOrUpdateResources(initialResolution, initialFormat, levels);
        if (!resourcesReady)
        {
            OutputTexture.Value = sourceTexture;
            return;
        }

        // --- Update Level Intensities if Shape or Levels changed ---
        // Check if recalculation is needed *after* InitializeOrUpdateResources ensured _lastLevels is current
        if (shape != _lastShape || levels != _lastLevels || _levelIntensities.Count != levels) // Check count too for safety
        {
            RecalculateLevelIntensities(levels, shape);
            _lastShape = shape; // Update tracking
        }

        // Check if calculation failed or lists mismatch
        if (_levelIntensities.Count != levels)
        {
            Log.Warning($"Bloom level intensity calculation failed or list size mismatch.", this);
            OutputTexture.Value = sourceTexture;
            return;
        }

        _stateBackup.Save(deviceContext);

        // --- Save State & Prepare Base State ---
        var vsStage = deviceContext.VertexShader;
        vsStage.Set(vs);
        //_currentState.VertexShader = vs;

        // ... (set common states) ...
        device.ImmediateContext.OutputMerger.BlendState = DefaultRenderingStates.DisabledBlendState;
        device.ImmediateContext.OutputMerger.DepthStencilState = DefaultRenderingStates.DisabledDepthStencilState;
        device.ImmediateContext.Rasterizer.State = DefaultRenderingStates.DefaultRasterizerState;
        //_currentState.RasterizerState = DefaultRenderingStates.DisabledRasterizerState;

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

        //var sourceSrv = ResourceManager.GetSrv(sourceTexture);

        // --- Pipeline Steps ---

        // 1. Bright Pass (Same as before)
        // ... set RTV, viewport, PS, Sampler, SRV, update/set CB ... draw ... unbind ...
        {
            /* ... Bright Pass Code ... */
            deviceContext.OutputMerger.SetTargets(_brightPassTarget.RTV);
            var viewport = new RawViewportF { X = 0, Y = 0, Width = initialResolution.Width, Height = initialResolution.Height, MinDepth = 0, MaxDepth = 1 };
            //deviceContext.Rasterizer.SetViewport(new RawViewportF(0, 0, _brightPassTarget.Resolution.Width, _brightPassTarget.Resolution.Height));
            deviceContext.Rasterizer.SetViewports([viewport]);

            deviceContext.PixelShader.Set(brightPassPS);
            deviceContext.PixelShader.SetSampler(0, linearSampler);
            deviceContext.PixelShader.SetShaderResource(0, sourceSrv);
            _thresholdParams.Threshold = threshold;

            ResourceManager.SetupConstBuffer(_thresholdParams, ref _thresholdParamsBuffer);
            //ResourceManager.UpdateConstBuffer(ref _thresholdParamsBuffer, _thresholdParams);

            deviceContext.PixelShader.SetConstantBuffer(0, _thresholdParamsBuffer);
            deviceContext.Draw(3, 0);
            deviceContext.PixelShader.SetShaderResource(0, null);
        }

        // 2. Downsample / Blur Pyramid (Same logic, result for level 'i' in _blurTargetsA[i].SRV)
        ShaderResourceView lastLevelSrv = _brightPassTarget.SRV;
        for (int level = 0; level < levels; ++level)
        {
            // ... (Downsample pass: lastLevelSrv -> _blurTargetsA[level]) ...
            // ... (Vertical Blur pass: _blurTargetsA[level].SRV -> _blurTargetsB[level].RTV) ...
            // ... (Horizontal Blur pass: _blurTargetsB[level].SRV -> _blurTargetsA[level].RTV) ...
            // ... (lastLevelSrv = _blurTargetsA[level].SRV) ...
            var targetSetA = _blurTargetsA[level];
            var targetSetB = _blurTargetsB[level];
            var levelResolution = targetSetA.Resolution;
            // Downsample
            {
                deviceContext.OutputMerger.SetTargets(targetSetA.RTV);

                //var rawViewportF = new RawViewportF(0, 0, levelResolution.Width, levelResolution.Height);
                deviceContext.Rasterizer.SetViewport(new RawViewportF { X = 0, Y = 0, Width = levelResolution.Width, Height = levelResolution.Height });
                deviceContext.PixelShader.Set(downsamplePS);
                deviceContext.PixelShader.SetSampler(0, linearSampler);
                deviceContext.PixelShader.SetShaderResource(0, lastLevelSrv);
                deviceContext.Draw(3, 0);
                deviceContext.PixelShader.SetShaderResource(0, null);
            }
            // Blur V (A->B)
            _blurParamsData.Width = levelResolution.Width;
            _blurParamsData.Height = levelResolution.Height;
            _blurParamsData.ClampTexture = clamp ? 1 : 0;
            _blurParamsData.UseMask = 0;
            _blurParamsData.MaskInvert = 0;

            {
                deviceContext.OutputMerger.SetTargets(targetSetB.RTV);
                deviceContext.PixelShader.Set(blurPS);
                deviceContext.PixelShader.SetSampler(0, linearSampler);
                deviceContext.PixelShader.SetShaderResource(0, targetSetA.SRV);
                _blurParamsData.DirX = 0.0f;
                _blurParamsData.DirY = blurOffset;
                ResourceManager.SetupConstBuffer(_blurParamsData, ref _blurParamsBuffer);
                deviceContext.PixelShader.SetConstantBuffer(0, _blurParamsBuffer);
                deviceContext.Draw(3, 0);
                deviceContext.PixelShader.SetShaderResource(0, null);
            }
            // Blur H (B->A)
            {
                deviceContext.OutputMerger.SetTargets(targetSetA.RTV);
                deviceContext.PixelShader.Set(blurPS);
                deviceContext.PixelShader.SetSampler(0, linearSampler);
                deviceContext.PixelShader.SetShaderResource(0, targetSetB.SRV);
                _blurParamsData.DirX = blurOffset;
                _blurParamsData.DirY = 0.0f;
                ResourceManager.SetupConstBuffer(_blurParamsData, ref _blurParamsBuffer);
                deviceContext.PixelShader.SetConstantBuffer(0, _blurParamsBuffer);
                deviceContext.Draw(3, 0);
                deviceContext.PixelShader.SetShaderResource(0, null);
            }
            lastLevelSrv = targetSetA.SRV; // Result for this level is in A
        }

        // 3. Initial Composite (Same as before)
        // ... copy sourceSRV -> _compositeTarget.RTV ...
        {
            /* ... Initial Composite Code ... */
            deviceContext.OutputMerger.SetTargets(_compositeTarget.RTV);
            deviceContext.OutputMerger.SetBlendState(DefaultRenderingStates.DisabledBlendState);
            deviceContext.Rasterizer.SetViewport(new RawViewportF
                                                     { X = 0, Y = 0, Width = _compositeTarget.Resolution.Width, Height = _compositeTarget.Resolution.Height });
            deviceContext.PixelShader.Set(copyPS);
            deviceContext.PixelShader.SetSampler(0, pointSampler);
            deviceContext.PixelShader.SetShaderResource(0, sourceSrv);
            deviceContext.Draw(3, 0);
            deviceContext.PixelShader.SetShaderResource(0, null);
        }

        // 4. Upsample and Additively Blend (Modified)
        {
            deviceContext.OutputMerger.SetBlendState(DefaultRenderingStates.AdditiveBlendState);
            deviceContext.PixelShader.Set(upsampleAddPS);
            deviceContext.PixelShader.SetSampler(0, linearSampler);
            deviceContext.OutputMerger.SetTargets(_compositeTarget.RTV); // Always write to composite
            deviceContext.Rasterizer.SetViewport(new RawViewportF
                                                     { X = 0, Y = 0, Width = _compositeTarget.Resolution.Width, Height = _compositeTarget.Resolution.Height });

            for (int level = levels - 1; level >= 0; --level)
            {
                var sourceSet = _blurTargetsA[level]; // Blurred result for this level

                // Calculate final intensity for this specific pass
                float normalizedLevelIntensity = _levelIntensities[level]; // Get precalculated value
                float finalPassIntensity = normalizedLevelIntensity * intensity; // Combine with overall intensity

                _compositeParamsData.PassIntensity = finalPassIntensity; // Use combined value
                _compositeParamsData.InvTargetSize = new Vector2(1.0f / _compositeTarget.Resolution.Width, 1.0f / _compositeTarget.Resolution.Height);
                _compositeParamsData.InvSourceSize = new Vector2(1.0f / sourceSet.Resolution.Width, 1.0f / sourceSet.Resolution.Height);
                ResourceManager.SetupConstBuffer(_compositeParamsData, ref _compositeParamsBuffer);
                deviceContext.PixelShader.SetConstantBuffer(0, _compositeParamsBuffer); // Assuming slot 0

                deviceContext.PixelShader.SetShaderResource(0, sourceSet.SRV); // Bind low-res texture
                deviceContext.Draw(3, 0);
                deviceContext.PixelShader.SetShaderResource(0, null); // Unbind
            }

            deviceContext.OutputMerger.SetBlendState(DefaultRenderingStates.DisabledBlendState); // Restore blend state
        }

        // --- Final Output ---
        OutputTexture.Value = _compositeTarget.Texture;
        _stateBackup.Restore(deviceContext);
    }

    // --- Internal Resources ---
    // RenderTargetSet struct and Lists _blurTargetsA, _blurTargetsB (Same as before)
    private sealed class RenderTargetSet
    {
        public Texture2D Texture;
        public RenderTargetView RTV;
        public ShaderResourceView SRV;
        public Size2 Resolution;

        public void Dispose()
        {
            Utilities.Dispose(ref Texture);
            Utilities.Dispose(ref RTV);
            Utilities.Dispose(ref SRV);
        }
    }

    private readonly List<RenderTargetSet> _blurTargetsA = new List<RenderTargetSet>();
    private readonly List<RenderTargetSet> _blurTargetsB = new List<RenderTargetSet>();
    private RenderTargetSet _brightPassTarget;
    private RenderTargetSet _compositeTarget;

    // Constant Buffers (CompositeParams modified)
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct ThresholdParams
    {
        [FieldOffset(0)]
        public float Threshold;

        public static readonly ThresholdParams Default = new ThresholdParams();
    }

    private ThresholdParams _thresholdParams;
    private Buffer _thresholdParamsBuffer;

    // BlurParameters struct and buffer (Same as before)
    [StructLayout(LayoutKind.Explicit, Size = (4 * 4) + (4 * 4))]
    private struct BlurParameters
    {
        /* ... */
        [FieldOffset(0 * 4)]
        public float DirX;

        [FieldOffset(1 * 4)]
        public float DirY;

        [FieldOffset(2 * 4)]
        public float Width;

        [FieldOffset(3 * 4)]
        public float Height;

        [FieldOffset(4 * 4)]
        public int UseMask;

        [FieldOffset(5 * 4)]
        public int MaskInvert;

        [FieldOffset(6 * 4)]
        public int ClampTexture;

        [FieldOffset(7 * 4)]
        public int _padding0;

        public static readonly BlurParameters Default = new BlurParameters();
    }

    private BlurParameters _blurParamsData;
    private Buffer _blurParamsBuffer;

    // CompositeParams struct modified to take final per-pass intensity
    [StructLayout(LayoutKind.Explicit, Size = 32)] // Ensure size/padding matches HLSL
    private struct CompositeParams
    {
        [FieldOffset(0)]
        public Vector2 InvTargetSize;

        [FieldOffset(8)]
        public Vector2 InvSourceSize;

        [FieldOffset(16)]
        public float PassIntensity; // Combined overall Intensity * normalized level weight

        public static readonly CompositeParams Default = new CompositeParams();
    }

    private CompositeParams _compositeParamsData;
    private Buffer _compositeParamsBuffer;

    // State Tracking 
    private Size2 _lastResolution = Size2.Zero;
    private Format _lastFormat = Format.Unknown;
    private int _lastLevels = -1;
    private float _lastShape = float.NaN; // Use NaN to ensure first calculation runs
    private readonly List<float> _levelIntensities = []; // Stores normalized intensity per level

    // --- Methods ---

    // Calculates normalized intensities based on level count and shape parameter
    private void RecalculateLevelIntensities(int numLevels, float shape)
    {
        _levelIntensities.Clear();
        if (numLevels <= 0) return;

        double sumT = 0;
        // Calculate sum of weights (using double for intermediate sum precision)
        for (int n = 1; n <= numLevels; ++n) // n=1 for level 0, n=2 for level 1 etc.
        {
            // Use System.Math.Pow for double precision power
            sumT += Math.Pow(n, shape);
        }

        // Calculate normalized intensity for each level
        double invSumT = (sumT > 1e-6) ? (1.0 / sumT) : 0.0; // Avoid division by zero or near-zero
        for (int n = 1; n <= numLevels; ++n)
        {
            double tN = Math.Pow(n, shape);
            float intensityN = (float)(tN * invSumT);
            _levelIntensities.Add(intensityN); // Add intensity for level (n-1)
        }

        Log.Debug($"Recalculated bloom level intensities (Levels={numLevels}, Shape={shape}): {string.Join(", ", _levelIntensities)}", this);
    }

    // Creates/updates ALL internal resources
    private bool InitializeOrUpdateResources(Size2 initialResolution, Format initialFormat, int numLevels)
    {
        // Check if general resource recreation is needed (resolution, format, levels changed, or resources missing)
        bool needsTextureRecreation = _compositeTarget?.Texture == null ||
                                      _compositeTarget.Texture.IsDisposed ||
                                      _brightPassTarget?.Texture == null ||
                                      _brightPassTarget.Texture.IsDisposed ||
                                      _blurTargetsA?.Count != numLevels ||
                                      _blurTargetsB.Count != numLevels ||
                                      _lastResolution != initialResolution ||
                                      _lastFormat != initialFormat ||
                                      _lastLevels != numLevels ||
                                      _blurTargetsA?.Count > 0 && (_blurTargetsA[0].Texture == null || _blurTargetsA[0].Texture.IsDisposed);

        // Check if constant buffers need creation (only happens once or after failure)
        bool needsBufferCreation = _thresholdParamsBuffer == null ||
                                   _thresholdParamsBuffer.IsDisposed ||
                                   _blurParamsBuffer == null ||
                                   _blurParamsBuffer.IsDisposed ||
                                   _compositeParamsBuffer == null ||
                                   _compositeParamsBuffer.IsDisposed;

        if (!needsTextureRecreation && !needsBufferCreation)
            return true; // Nothing to do

        // --- Cleanup and Recreate ---
        if (needsTextureRecreation)
        {
            Log.Debug($"Recreating Bloom textures/views for {initialResolution.Width}x{initialResolution.Height}, {numLevels} levels, Format: {initialFormat}",
                      this);
            CleanupResources(); // Cleans textures, views, AND buffers, resets state tracking
        }
        else if (needsBufferCreation) // Only buffers need creation, textures are fine
        {
            Log.Debug($"Recreating Bloom constant buffers.", this);
            // Only dispose buffers
            Utilities.Dispose(ref _thresholdParamsBuffer);
            Utilities.Dispose(ref _blurParamsBuffer);
            Utilities.Dispose(ref _compositeParamsBuffer);
        }

        try
        {
            var device = ResourceManager.Device;

            // --- Recreate Textures/Views if needed ---
            if (needsTextureRecreation)
            {
                // Create Full Resolution Targets
                var fullResDesc = new Texture2DDescription
                                      {
                                          /* ... as before ... */
                                          Width = initialResolution.Width, Height = initialResolution.Height, Format = initialFormat,
                                          BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource, Usage = ResourceUsage.Default,
                                          CpuAccessFlags = CpuAccessFlags.None, OptionFlags = ResourceOptionFlags.None, MipLevels = 1,
                                          ArraySize = 1, SampleDescription = new SampleDescription(1, 0)
                                      };
                _brightPassTarget = CreateRenderTargetSet(device, fullResDesc);
                _compositeTarget = CreateRenderTargetSet(device, fullResDesc);

                // Create Downsample Pyramid Targets
                Size2 currentResolution = initialResolution;
                for (int level = 0; level < numLevels; ++level)
                {
                    currentResolution.Width = Math.Max(1, currentResolution.Width / 2);
                    currentResolution.Height = Math.Max(1, currentResolution.Height / 2);
                    var levelDesc = fullResDesc;
                    levelDesc.Width = currentResolution.Width;
                    levelDesc.Height = currentResolution.Height;
                    _blurTargetsA.Add(CreateRenderTargetSet(device, levelDesc));
                    _blurTargetsB.Add(CreateRenderTargetSet(device, levelDesc));
                }

                // Update tracking info after successful texture creation
                _lastResolution = initialResolution;
                _lastFormat = initialFormat;
                _lastLevels = numLevels;
            }

            // --- Create Constant Buffers if needed ---
            if (_thresholdParamsBuffer == null) ResourceManager.SetupConstBuffer(ThresholdParams.Default, ref _thresholdParamsBuffer);
            if (_blurParamsBuffer == null) ResourceManager.SetupConstBuffer(BlurParameters.Default, ref _blurParamsBuffer);
            if (_compositeParamsBuffer == null) ResourceManager.SetupConstBuffer(CompositeParams.Default, ref _compositeParamsBuffer);

            // Final check if all buffers were created
            if (_thresholdParamsBuffer == null || _blurParamsBuffer == null || _compositeParamsBuffer == null)
                throw new Exception("Failed to create one or more constant buffers.");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to create Bloom resources: {e.Message}", this);
            CleanupResources(); // Cleanup partially created resources
            return false;
        }

        return true;
    }

    // Helper to create a RenderTargetSet
    private RenderTargetSet CreateRenderTargetSet(Device device, Texture2DDescription desc)
    {
        var set = new RenderTargetSet { Resolution = new Size2(desc.Width, desc.Height) };
        //set.Texture = new Texture2D(device, desc);
        set.Texture = Texture2D.CreateTexture2D(desc);
        set.RTV = new RenderTargetView(device, set.Texture);
        set.SRV = new ShaderResourceView(device, set.Texture);
        return set;
    }

    // CleanupResources disposes everything and resets state
    private void CleanupResources()
    {
        // ... (Dispose textures/views in lists _blurTargetsA/B, _brightPassTarget, _compositeTarget) ...
        foreach (var set in _blurTargetsA)
        {
            Utilities.Dispose(ref set.RTV);
            Utilities.Dispose(ref set.SRV);
            Utilities.Dispose(ref set.Texture);
        }

        _blurTargetsA.Clear();
        foreach (var set in _blurTargetsB)
        {
            Utilities.Dispose(ref set.RTV);
            Utilities.Dispose(ref set.SRV);
            Utilities.Dispose(ref set.Texture);
        }

        _blurTargetsB.Clear();
        _brightPassTarget?.Dispose();
        _compositeTarget?.Dispose();

        // ... (Dispose constant buffers) ...
        Utilities.Dispose(ref _thresholdParamsBuffer);
        Utilities.Dispose(ref _blurParamsBuffer);
        Utilities.Dispose(ref _compositeParamsBuffer);

        // ... (Reset state tracking) ...
        _lastResolution = Size2.Zero;
        _lastFormat = Format.Unknown;
        _lastLevels = -1;
        _lastShape = float.NaN;
        _levelIntensities.Clear();

    }

    private readonly D3D11StateBackup _stateBackup = new();

    protected override void Dispose(bool disposing)
    {
        /* ... Call CleanupResources ... */
        if (disposing)
        {
            CleanupResources();
            _stateBackup?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Helper class to save and restore specific D3D11 pipeline states
    /// commonly modified by fullscreen post-processing effects.
    /// </summary>
    private sealed class D3D11StateBackup : System.IDisposable
    {
        // --- Saved State Members ---

        // Input Assembler
        private PrimitiveTopology _topology;

        // Vertex Shader (minimal - just shader, assuming VS inputs aren't changed)
        private SharpDX.Direct3D11.VertexShader _vertexShader;

        // Geometry Shader (minimal - just shader)
        private SharpDX.Direct3D11.GeometryShader _geometryShader;

        // Pixel Shader (Shader, 1 CB, 2 SRVs, 1 Sampler)
        private SharpDX.Direct3D11.PixelShader _pixelShader;
        private SharpDX.Direct3D11.Buffer[] _psConstantBuffers = new Buffer[1]; // Slot 0
        private SharpDX.Direct3D11.ShaderResourceView[] _psShaderResourceViews = new ShaderResourceView[2]; // Slots 0, 1
        private SharpDX.Direct3D11.SamplerState[] _psSamplerStates = new SamplerState[1]; // Slot 0

        // Rasterizer Stage
        private SharpDX.Direct3D11.RasterizerState _rasterizerState;
        private RawViewportF[] _viewports; // Need to save all active viewports

        // Output Merger Stage
        private SharpDX.Direct3D11.BlendState _blendState;
        private RawColor4 _blendFactor;
        private int _sampleMask;
        private SharpDX.Direct3D11.DepthStencilState _depthStencilState;
        private int _stencilRef;

        private SharpDX.Direct3D11.RenderTargetView[]
            _renderTargetViews = new RenderTargetView[OutputMergerStage.SimultaneousRenderTargetCount]; // Save max possible RTVs

        private SharpDX.Direct3D11.DepthStencilView _depthStencilView;

        private bool _isSaved;

        /// <summary>
        /// Saves the relevant D3D11 pipeline states modified by the effect.
        /// </summary>
        public void Save(DeviceContext context)
        {
            if (_isSaved) return; // Prevent double saves without restore

            // IA
            _topology = context.InputAssembler.PrimitiveTopology;

            // Shaders (Get the objects)
            var vsStage = context.VertexShader;
            _vertexShader = vsStage.Get();
            _geometryShader = context.GeometryShader.Get(); // Save even if we set to null later

            // Pixel Shader Resources 
            var psStage = context.PixelShader;
            _pixelShader = psStage.Get();
            _psConstantBuffers = psStage.GetConstantBuffers(0, 1);
            _psShaderResourceViews = psStage.GetShaderResources(0, 2);
            _psSamplerStates = psStage.GetSamplers(0, 1);

            // Rasterizer
            _rasterizerState = context.Rasterizer.State;
            _viewports = context.Rasterizer.GetViewports<RawViewportF>(); // Get all active

            // Output Merger
            _blendState = context.OutputMerger.GetBlendState(out _blendFactor, out _sampleMask);
            _depthStencilState = context.OutputMerger.GetDepthStencilState(out _stencilRef);
            
            // Suggested API doesn't exist.
            //context.OutputMerger.GetRenderTargets(OutputMergerStage.SimultaneousRenderTargetCount, _renderTargetViews, out _depthStencilView);
            _renderTargetViews = context.OutputMerger.GetRenderTargets(1);
            context.OutputMerger.GetRenderTargets(out _depthStencilView);
            _isSaved = true;
        }

        /// <summary>
        /// Restores the previously saved D3D11 pipeline state.
        /// Also disposes the state objects retrieved during Save().
        /// </summary>
        public void Restore(DeviceContext context)
        {
            if (!_isSaved) return; // Nothing to restore

            // IA
            context.InputAssembler.PrimitiveTopology = _topology;

            // Shaders
            context.VertexShader.Set(_vertexShader);
            context.GeometryShader.Set(_geometryShader);

            // Pixel Shader Resources
            var psStage = context.PixelShader;
            psStage.Set(_pixelShader);
            psStage.SetConstantBuffers(0, _psConstantBuffers.Length, _psConstantBuffers);
            psStage.SetShaderResources(0, _psShaderResourceViews.Length, _psShaderResourceViews);
            psStage.SetSamplers(0, _psSamplerStates.Length, _psSamplerStates);

            // Rasterizer
            context.Rasterizer.State = _rasterizerState;
            context.Rasterizer.SetViewports(_viewports, _viewports?.Length ?? 0); // SetViewports handles null array / count 0 correctly
            _viewports = null; // Clear local reference

            // Output Merger
            context.OutputMerger.SetBlendState(_blendState, _blendFactor, _sampleMask);
            //context.OutputMerger.SetDepthStencilState(_depthStencilState, _stencilRef);
            //context.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetViews); // SetRenderTargets handles null DSV
            if (_renderTargetViews.Length > 0)
                context.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetViews);
            
            foreach (var rtv in _renderTargetViews)
                rtv?.Dispose();                 
            
            _isSaved = false;

            Dispose();
        }

        /// <summary>
        /// Disposes potentially held state objects if Restore wasn't called.
        /// </summary>
        public void Dispose()
        {
            // If Save was called but Restore wasn't, dispose the retrieved state objects
            if (_isSaved)
            {
                Utilities.Dispose(ref _vertexShader);
                Utilities.Dispose(ref _geometryShader);
                Utilities.Dispose(ref _pixelShader);
                for (int i = 0; i < _psConstantBuffers.Length; ++i)
                {
                    Utilities.Dispose(ref _psConstantBuffers[i]);
                    _psConstantBuffers[i] = null;
                }
                // Don't dispose SRVs in _psShaderResourceViews array

                for (int i = 0; i < _psSamplerStates.Length; ++i)
                {
                    Utilities.Dispose(ref _psSamplerStates[i]);
                    _psSamplerStates[i] = null;
                }

                Utilities.Dispose(ref _rasterizerState);
                Utilities.Dispose(ref _blendState);
                Utilities.Dispose(ref _depthStencilState);

                for (int i = 0; i < _renderTargetViews.Length; ++i)
                {
                    Utilities.Dispose(ref _renderTargetViews[i]);
                    _renderTargetViews[i] = null;
                }

                Utilities.Dispose(ref _depthStencilView);
            }

            // Nullify references
            _vertexShader = null;
            _pixelShader = null;
            _geometryShader = null; /* etc. */
            _psShaderResourceViews[0] = null;
            _psShaderResourceViews[1] = null;
            _viewports = null;
            _isSaved = false;
        }
    }

    [Input(Guid = "692BC2F0-68F2-45CA-A0FB-CD1C5D08E982")]
    public readonly InputSlot<T3.Core.DataTypes.Texture2D> SourceTexture = new InputSlot<T3.Core.DataTypes.Texture2D>();

    [Input(Guid = "98E88D02-3B78-403C-B9C9-B5ECF8565ACD")]
    public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> SourceTextureSrv = new InputSlot<SharpDX.Direct3D11.ShaderResourceView>();

    [Input(Guid = "D9576354-76F1-403C-8B28-45386F657190")]
    public readonly InputSlot<float> Threshold = new InputSlot<float>();

    [Input(Guid = "0F6919CD-33C9-4E99-AD72-B2A8DCB2B7A2")]
    public readonly InputSlot<float> Intensity = new InputSlot<float>();

    [Input(Guid = "4B1AC17B-B54C-4741-BB38-5784F6C1891A")]
    public readonly InputSlot<float> BlurOffset = new InputSlot<float>();

    [Input(Guid = "388489EE-EA0C-49A3-AEE8-1AE2C4DCBDA2")]
    public readonly InputSlot<int> Levels = new InputSlot<int>();

    [Input(Guid = "85859C09-C1E5-4452-8B79-4470CC3DAAA8")]
    public readonly InputSlot<float> Shape = new InputSlot<float>();

    [Input(Guid = "D5914036-F628-4305-D7B5-1634B219C305")]
    public readonly InputSlot<bool> Clamp = new InputSlot<bool>();

    [Input(Guid = "E6A25147-0739-4416-E8C6-2745C32AD416")]
    public readonly InputSlot<T3.Core.DataTypes.VertexShader> FullscreenVS = new InputSlot<T3.Core.DataTypes.VertexShader>();

    [Input(Guid = "F7B36258-184A-4527-F9D7-3856D43BE527")]
    public readonly InputSlot<T3.Core.DataTypes.PixelShader> BrightPassPS = new InputSlot<T3.Core.DataTypes.PixelShader>();

    [Input(Guid = "08C47369-295B-4638-0AE8-4967E54CF638")]
    public readonly InputSlot<T3.Core.DataTypes.PixelShader> DownsamplePS = new InputSlot<T3.Core.DataTypes.PixelShader>();

    [Input(Guid = "19D5847A-3A6C-4749-1BF9-5A78F65D0749")]
    public readonly InputSlot<T3.Core.DataTypes.PixelShader> SeparableBlurPS = new InputSlot<T3.Core.DataTypes.PixelShader>();

    [Input(Guid = "2AE6958B-4B7D-485A-2C0A-6B89076E185A")]
    public readonly InputSlot<T3.Core.DataTypes.PixelShader> UpsampleAddPS = new InputSlot<T3.Core.DataTypes.PixelShader>();

    [Input(Guid = "3BF7A69C-5C8E-496B-3D1B-7C9A187F296B")]
    public readonly InputSlot<T3.Core.DataTypes.PixelShader> CopyPS = new InputSlot<T3.Core.DataTypes.PixelShader>();

    [Input(Guid = "5D19C8BE-7EA0-4B8D-5F3D-9EBB3A914B8D")]
    public readonly InputSlot<SharpDX.Direct3D11.SamplerState> LinearSampler = new InputSlot<SharpDX.Direct3D11.SamplerState>();

    [Input(Guid = "4C08B7AD-6D9F-4A7C-4E2C-8DAA29803A7C")]
    public readonly InputSlot<SharpDX.Direct3D11.SamplerState> PointSampler = new InputSlot<SharpDX.Direct3D11.SamplerState>();
}