using System.Diagnostics;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.Windows;
using Device = SharpDX.Direct3D11.Device;
using Texture2D = T3.Core.DataTypes.Texture2D;
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Editor.Gui.OutputUi
{
    internal class CommandOutputUi : OutputUi<Command>
    {
        public bool GizmosEnabled { get; set; } = false;

        internal CommandOutputUi()
        {
            // ensure op exists for drawing grid
            var outputWindowGridSymbolId = Guid.Parse("e5588101-5686-4b02-ab7d-e58199ba552e");
            
            if(!SymbolRegistry.TryGetSymbol(outputWindowGridSymbolId, out var outputWindowGridSymbol))
            {
                Log.Warning("CommandOutputUi: Could not find grid Gizmo symbol UI");
                return;
            }

            if (!outputWindowGridSymbol.TryCreateParentlessInstance(out var gridInstance))
            {
                var message = $"{nameof(CommandOutputUi)} Could not create grid instance";
                Log.Error(message);
                throw new Exception(message);
            }

            _gridOutputs = gridInstance.Outputs;
        }

        protected override void Recompute(ISlot slot, EvaluationContext context)
        {
            var originalCamMatrix = context.WorldToCamera;
            var originalViewMatrix = context.CameraToClipSpace;
            
            // invalidate
            StartInvalidation(slot);

            // setup render target - TODO: this should not be done for all 'Command' outputs as most of them don't produce image content
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;

            Int2 size = context.RequestedResolution;
            var wasRebuild = UpdateTextures(device, size, Format.R16G16B16A16_Float);
            var deviceContext = device.ImmediateContext;
            var prevViewports = deviceContext.Rasterizer.GetViewports<RawViewportF>();
            var prevTargets = deviceContext.OutputMerger.GetRenderTargets(1);
            deviceContext.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));
            //deviceContext.OutputMerger.SetTargets(_colorBufferRtv);
            deviceContext.OutputMerger.SetTargets(_depthBufferDsv, _colorBufferRtv);

            //var colorRgba = new RawColor4(0.1f, 0.1f, 0.1f, 1.0f);
            var colorRgba = new RawColor4(context.BackgroundColor.X,
                                          context.BackgroundColor.Y,
                                          context.BackgroundColor.Z,
                                          context.BackgroundColor.W);
            deviceContext.ClearRenderTargetView(_colorBufferRtv, colorRgba);
            if(_depthBufferDsv != null)
                deviceContext.ClearDepthStencilView(_depthBufferDsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            
            // evaluate the op
            slot.Update(context);

            if (context.ShowGizmos != T3.Core.Operator.GizmoVisibility.Off)
            {
                context.WorldToCamera = originalCamMatrix;
                context.CameraToClipSpace = originalViewMatrix;

                var outputSlot = _gridOutputs[0];
                outputSlot.Invalidate();
                outputSlot.Update(context);
            }

            // restore prev setup
            deviceContext.Rasterizer.SetViewports(prevViewports);
            deviceContext.OutputMerger.SetTargets(prevTargets);

            // clean up ref counts for RTVs
            for (int i = 0; i < prevTargets.Length; i++)
            {
                prevTargets[i].Dispose();
            }
        }

        public override IOutputUi Clone()
        {
            return new CommandOutputUi()
                       {
                           OutputDefinition = OutputDefinition,
                           PosOnCanvas = PosOnCanvas,
                           Size = Size
                       };
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<Command>)
            {
                ImageOutputCanvas.Current.DrawTexture(_colorBuffer);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private bool UpdateTextures(Device device, Int2 size, Format format)
        {
            try
            {

                // Initialize color buffer
                {
                    if (_colorBuffer != null)
                    {
                        var currentColorDesc = _colorBuffer.Description;
                        if (currentColorDesc.Width == size.Width
                            && currentColorDesc.Height == size.Height
                            && currentColorDesc.Format == format)
                            return false; // nothing changed
                        
                        _colorBuffer.Dispose();
                    }

                    _colorBufferSrv?.Dispose();
                    _colorBufferRtv?.Dispose();

                    var colorDesc = DefaultColorDescription with 
                                        {
                                            Format = format,
                                            Width = size.Width,
                                            Height = size.Height,
                                        };
                    
                    _colorBuffer = ResourceManager.CreateTexture2D(colorDesc);
                    ResourceManager.CreateShaderResourceView(_colorBuffer, null, ref _colorBufferSrv);
                    ResourceManager.CreateRenderTargetView(_colorBuffer, null, ref _colorBufferRtv);
                }

                // Initialize depth buffer 
                {
                    Utilities.Dispose(ref _depthBufferDsv);
                    Utilities.Dispose(ref _depthBuffer);

                    var depthDesc = DefaultDepthDescription with
                                        {
                                            Width = size.Width,
                                            Height = size.Height,
                                        };

                    _depthBuffer = ResourceManager.CreateTexture2D(depthDesc);
                    var depthViewDesc = new DepthStencilViewDescription
                                            {
                                                Format = Format.D32_Float,
                                                Dimension = DepthStencilViewDimension.Texture2D
                                            };
                    
                    _depthBufferDsv?.Dispose();
                    _depthBufferDsv = new DepthStencilView(device, _depthBuffer, depthViewDesc);
                    //Log.Debug("new depth stencil view");
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to generate texture: " + e.Message);
                return false;
            }
            
            return true;
        }

        private static readonly Texture2DDescription DefaultColorDescription = new()
                                                                                   {
                                                                                       ArraySize = 1,
                                                                                       BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                                                                       CpuAccessFlags = CpuAccessFlags.None,
                                                                                       MipLevels = 1,
                                                                                       OptionFlags = ResourceOptionFlags.None,
                                                                                       SampleDescription = new SampleDescription(1, 0),
                                                                                       Usage = ResourceUsage.Default
                                                                                   };

        private static readonly Texture2DDescription DefaultDepthDescription = new()
                                                                                   {
                                                                                       ArraySize = 1,
                                                                                       BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                                                                                       CpuAccessFlags = CpuAccessFlags.None,
                                                                                       Format = Format.R32_Typeless,
                                                                                       Width = 1,
                                                                                       Height = 1,
                                                                                       MipLevels = 1,
                                                                                       OptionFlags = ResourceOptionFlags.None,
                                                                                       SampleDescription = new SampleDescription(1, 0),
                                                                                       Usage = ResourceUsage.Default
                                                                                   };
        private Texture2D _colorBuffer;
        private ShaderResourceView _colorBufferSrv;
        
        private RenderTargetView _colorBufferRtv;
        private Texture2D _depthBuffer;
        
        private DepthStencilView _depthBufferDsv;
        private IReadOnlyList<ISlot> _gridOutputs;
    }
}