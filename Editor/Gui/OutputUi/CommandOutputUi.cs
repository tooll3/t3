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
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Editor.Gui.OutputUi
{
    public class CommandOutputUi : OutputUi<Command>
    {
        public bool GizmosEnabled { get; set; } = false;

        public CommandOutputUi()
        {
            // ensure op exists for drawing grid
            var gridPlaneGizmoId = Guid.Parse("e5588101-5686-4b02-ab7d-e58199ba552e");
            var gridPlaneGizmoSymbol = SymbolRegistry.Entries[gridPlaneGizmoId];

            var childId = Guid.NewGuid();
            var gridSymbolChild = new SymbolChild(gridPlaneGizmoSymbol, childId, null);

            if (!Symbol.TryCreateInstance(null, gridSymbolChild, out var gridInstance, out var reason))
            {
                Log.Error(nameof(CommandOutputUi) + ": " + reason);
                gridInstance = gridPlaneGizmoSymbol.InstancesOfSymbol.Single(x => x.SymbolChildId == childId);
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
            if (slot is Slot<Command> typedSlot)
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
                    if (_colorBuffer != null
                        && _colorBuffer.Description.Width == size.Width
                        && _colorBuffer.Description.Height == size.Height
                        && _colorBuffer.Description.Format == format)
                        return false; // nothing changed

                    _colorBuffer?.Dispose();
                    _colorBufferSrv?.Dispose();
                    _colorBufferRtv?.Dispose();

                    var colorDesc = new Texture2DDescription()
                                        {
                                            ArraySize = 1,
                                            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                            CpuAccessFlags = CpuAccessFlags.None,
                                            Format = format,
                                            Width = size.Width,
                                            Height = size.Height,
                                            MipLevels = 1,
                                            OptionFlags = ResourceOptionFlags.None,
                                            SampleDescription = new SampleDescription(1, 0),
                                            Usage = ResourceUsage.Default
                                        };
                    _colorBuffer = new Texture2D(device, colorDesc);
                    _colorBufferSrv = new ShaderResourceView(device, _colorBuffer);
                    _colorBufferRtv = new RenderTargetView(device, _colorBuffer);
                }

                // Initialize depth buffer 
                {
                    Utilities.Dispose(ref _depthBufferDsv);
                    Utilities.Dispose(ref _depthBuffer);

                    var depthDesc = new Texture2DDescription()
                                        {
                                            ArraySize = 1,
                                            BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                                            CpuAccessFlags = CpuAccessFlags.None,
                                            Format = Format.R32_Typeless,
                                            Width = size.Width,
                                            Height = size.Height,
                                            MipLevels = 1,
                                            OptionFlags = ResourceOptionFlags.None,
                                            SampleDescription = new SampleDescription(1, 0),
                                            Usage = ResourceUsage.Default
                                        };

                    _depthBuffer = new Texture2D(device, depthDesc);
                    var depthViewDesc = new DepthStencilViewDescription()
                                            {
                                                Format = Format.D32_Float,
                                                Dimension = DepthStencilViewDimension.Texture2D
                                            };
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

        private Texture2D _colorBuffer;
        private ShaderResourceView _colorBufferSrv;
        
        private RenderTargetView _colorBufferRtv;
        private Texture2D _depthBuffer;
        
        private DepthStencilView _depthBufferDsv;
        private IReadOnlyList<ISlot> _gridOutputs;
    }
}