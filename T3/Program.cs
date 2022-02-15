using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using t3.App;
using T3.Compilation;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using Device = SharpDX.Direct3D11.Device;

namespace T3
{
    public static class Program
    {
        private static T3RenderForm _t3RenderForm;
        public static Device Device { get; private set; }
        public static bool IsFullScreenRequested { get; set; } = false;

        [STAThread]
        private static void Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();

            _main.CreateRenderForm("T3", false);

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, _main.SwapChainDescription, out var device, out _main.SwapChain);
            _deviceContext = device.ImmediateContext;
            Device = device;
            Factory factory = _main.SwapChain.GetParent<Factory>();

            // Ignore all windows events
            factory.MakeWindowAssociation(_main.Form.Handle, WindowAssociationFlags.IgnoreAll);

            _t3RenderForm = new T3RenderForm(device, _main.Form.Width, _main.Form.Height);

            // Initialize T3 main window
            _main.InitRenderTargetsAndEventHandlers(device);
            _main.Form.KeyDown += HandleKeyDown;
            _main.Form.KeyUp += HandleKeyUp;
            _main.Form.Closing += (sender, args) =>
                                  {
                                      args.Cancel = T3Ui.UiModel.IsSaving;
                                      Log.Debug($"Cancel closing because save-operation is in progress.");
                                  };

            _main.Form.WindowState = FormWindowState.Maximized;

            // Initialize optional Viewer Windows
            _viewer.CreateRenderForm("T3 Viewer", true);
            _viewer.InitViewSwapChain(factory, device);
            _viewer.InitRenderTargetsAndEventHandlers(device);
            _viewer.Form.Show();

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            SharedResources.Initialize(resourceManager);

            _t3ui = new T3Ui();
            
            // Setup file watching the operator source
            resourceManager.OperatorsAssembly = T3Ui.UiModel.OperatorsAssembly;
            foreach (var (_, symbol) in SymbolRegistry.Entries)
            {
                ResourceManager.Instance().CreateOperatorEntry(@"Operators\Types\" + symbol.Name + ".cs", symbol.Id.ToString(), OperatorUpdating.Update);
            }

            Console.WriteLine($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");
            ShaderResourceView viewWindowBackgroundSrv = null;

            unsafe
            {
                // Disable ImGui ini file settings
                ImGui.GetIO().NativePtr->IniFilename = null;
            }

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            startupStopWatch.Stop();
            Log.Debug($"startup took {startupStopWatch.ElapsedMilliseconds}ms.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Int64 lastElapsedTicks = stopwatch.ElapsedTicks;

            // Main loop
            void RenderCallback()
            {
                Int64 ticks = stopwatch.ElapsedTicks;
                Int64 ticksDiff = ticks - lastElapsedTicks;
                ImGui.GetIO().DeltaTime = (float)((double)(ticksDiff) / Stopwatch.Frequency);
                lastElapsedTicks = ticks;
                ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(_main.Form.ClientSize.Width, _main.Form.ClientSize.Height);

                HandleFullscreenToggle();

                //NodeOperations.UpdateChangedOperators();
                var modifiedSymbols = resourceManager.UpdateChangedOperatorTypes();
                foreach (var symbol in modifiedSymbols)
                {
                    UiModel.UpdateUiEntriesForSymbol(symbol);
                }

                DirtyFlag.IncrementGlobalTicks();
                T3Metrics.UiRenderingStarted();
                T3Style.Apply();

                ImGui.NewFrame();
                _main.PrepareRenderingFrame(_deviceContext);

                // Render 2nd view
                _viewer.Form.Visible = T3Ui.ShowSecondaryRenderWindow;
                if (T3Ui.ShowSecondaryRenderWindow)
                {
                    _viewer.PrepareRenderingFrame(_deviceContext);

                    if (resourceManager.Resources[SharedResources.FullScreenVertexShaderId] is VertexShaderResource vsr)
                        _deviceContext.VertexShader.Set(vsr.VertexShader);

                    if (resourceManager.Resources[SharedResources.FullScreenPixelShaderId] is PixelShaderResource psr)
                        _deviceContext.PixelShader.Set(psr.PixelShader);

                    if (resourceManager.SecondRenderWindowTexture != null && !resourceManager.SecondRenderWindowTexture.IsDisposed)
                    {
                        Log.Debug($"using TextureId:{resourceManager.SecondRenderWindowTexture}, debug name:{resourceManager.SecondRenderWindowTexture.DebugName}");
                        if (viewWindowBackgroundSrv == null || viewWindowBackgroundSrv.Resource.NativePointer != resourceManager.SecondRenderWindowTexture.NativePointer)
                        {
                            viewWindowBackgroundSrv?.Dispose();
                            viewWindowBackgroundSrv = new ShaderResourceView(device, resourceManager.SecondRenderWindowTexture);
                        }

                        _deviceContext.Rasterizer.State = SharedResources.ViewWindowRasterizerState;
                        _deviceContext.PixelShader.SetShaderResource(0, viewWindowBackgroundSrv);
                    }
                    else if (resourceManager.Resources[ SharedResources.ViewWindowDefaultSrvId] is ShaderResourceViewResource srvr)
                    {
                        _deviceContext.PixelShader.SetShaderResource(0, srvr.ShaderResourceView);
                        Log.Debug($"using Default TextureId:{srvr.TextureId}, debug name:{srvr.ShaderResourceView.DebugName}");
                    }
                    else
                    {
                        Log.Debug("invalid srv for 2nd render view");
                    }

                    _deviceContext.Draw(3, 0);
                    _deviceContext.PixelShader.SetShaderResource(0, null);
                }

                _t3ui.Draw();

                _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, _main.Form.ClientSize.Width, _main.Form.ClientSize.Height, 0.0f, 1.0f));
                _deviceContext.OutputMerger.SetTargets(_main.RenderTargetView);

                ImGui.Render();
                _t3RenderForm.RenderImDrawData(ImGui.GetDrawData());

                T3Metrics.UiRenderingCompleted();

                _main.SwapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);

                if (T3Ui.ShowSecondaryRenderWindow)
                    _viewer.SwapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);
            }

            RenderLoop.Run(_main.Form, RenderCallback);

            try
            {
                _t3RenderForm.Dispose();
            }
            catch (Exception e)
            {
                Log.Warning("Exception during shutdown: " + e);
            }

            // Release all resources
            _main.RenderTargetView.Dispose();
            _main.BackBufferTexture.Dispose();
            _deviceContext.ClearState();
            _deviceContext.Flush();
            device.Dispose();
            _deviceContext.Dispose();
            _main.SwapChain.Dispose();
            factory.Dispose();
        }

        private static void HandleFullscreenToggle()
        {
            var isBorderStyleFullScreen = _main.Form.FormBorderStyle == FormBorderStyle.None;
            if (isBorderStyleFullScreen == IsFullScreenRequested)
                return;

            if (IsFullScreenRequested)
            {
                _main.Form.FormBorderStyle = FormBorderStyle.Sizable;
                _main.Form.WindowState = FormWindowState.Normal;
                _main.Form.FormBorderStyle = FormBorderStyle.None;
                var screenIndexForMainScreen = UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen ? 1 : 0;
                var screenIndexFor2ndScreen = UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen ? 0 : 1;
                ;
                _main.Form.Bounds = Screen.AllScreens[screenIndexForMainScreen].Bounds;

                if (T3Ui.ShowSecondaryRenderWindow)
                {
                    _viewer.Form.WindowState = FormWindowState.Normal;
                    _viewer.Form.FormBorderStyle = FormBorderStyle.None;
                    _viewer.Form.Bounds = Screen.AllScreens[screenIndexFor2ndScreen].Bounds;
                }
                else
                {
                    _viewer.Form.WindowState = FormWindowState.Normal;
                    _viewer.Form.FormBorderStyle = FormBorderStyle.None;
                    _viewer.Form.Bounds = Screen.AllScreens[screenIndexForMainScreen].Bounds;
                }
            }
            else
            {
                _main.Form.FormBorderStyle = FormBorderStyle.Sizable;
                _viewer.Form.FormBorderStyle = FormBorderStyle.Sizable;
            }
            //_mainWindow.RenderForm.FormBorderStyle = isFullScreenBorderStyle ? FormBorderStyle.Sizable : FormBorderStyle.None;
            //_viewerWindow.RenderForm.FormBorderStyle = fullScreenBorderStyle ? FormBorderStyle.Sizable : FormBorderStyle.None;
        }

        private static void HandleKeyDown(object sender, KeyEventArgs e)
        {
            var keyIndex = (int)e.KeyCode;
            if (keyIndex >= Core.IO.KeyHandler.PressedKeys.Length)
            {
                Log.Warning($"Ignoring out of range key code {e.KeyCode} with index {keyIndex}");
            }
            else
            {
                Core.IO.KeyHandler.PressedKeys[keyIndex] = true;
            }
        }

        private static void HandleKeyUp(object sender, KeyEventArgs e)
        {
            var keyIndex = (int)e.KeyCode;
            if (keyIndex < Core.IO.KeyHandler.PressedKeys.Length)
            {
                Core.IO.KeyHandler.PressedKeys[keyIndex] = false;
            }
        }

        private static readonly AppWindow _main = new();
        private static readonly AppWindow _viewer = new();

        private static T3Ui _t3ui = null;
        private static DeviceContext _deviceContext;

    }

    /// <summary>
    /// A collection of rendering resource used across the T3 UI
    /// </summary>
    public static class SharedResources
    {
        public static void Initialize(ResourceManager resourceManager)
        {
            FullScreenVertexShaderId =
                resourceManager.CreateVertexShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "vsMain", "vs-fullscreen-texture", () => { });
            FullScreenPixelShaderId =
                resourceManager.CreatePixelShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "psMain", "ps-fullscreen-texture", () => { });
            
            ViewWindowRasterizerState = new RasterizerState(ResourceManager.Instance().Device, new RasterizerStateDescription
                                                                                {
                                                                                    FillMode = FillMode.Solid, // Wireframe
                                                                                    CullMode = CullMode.None,
                                                                                    IsFrontCounterClockwise = true,
                                                                                    DepthBias = 0,
                                                                                    DepthBiasClamp = 0,
                                                                                    SlopeScaledDepthBias = 0,
                                                                                    IsDepthClipEnabled = false,
                                                                                    IsScissorEnabled = default,
                                                                                    IsMultisampleEnabled = false,
                                                                                    IsAntialiasedLineEnabled = false
                                                                                }); 
            

            (uint texId, var tmpId ) = resourceManager.CreateTextureFromFile(@"Resources\images\chipmunk.jpg", null);
            ViewWindowDefaultSrvId = tmpId;
        }
        
        public static uint FullScreenVertexShaderId;
        public static uint FullScreenPixelShaderId;
        public static RasterizerState ViewWindowRasterizerState;
        public static uint ViewWindowDefaultSrvId;
    }
}