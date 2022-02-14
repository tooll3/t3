using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SharpDX.Mathematics.Interop;
using T3.Compilation;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui;
using T3.Gui.Graph.Interaction;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Resource = T3.Core.Resource;

namespace T3
{
    class ImGuiDx11RenderForm : RenderForm
    {
        public ImGuiDx11RenderForm(string title)
            : base(title)
        {
            MouseMove += (o, e) => ImGui.GetIO().MousePos = new System.Numerics.Vector2(e.X, e.Y);
        }

        #region WM Message Ids
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_RBUTTONDBLCLK = 0x0206;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MBUTTONDBLCLK = 0x0209;

        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_CHAR = 0x0102;
        private const int WM_SETCURSOR = 0x0020;

        private const int WM_SETFOCUS = 0x0007;
        #endregion

        #region VK constants
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_ALT = 0x12;
        #endregion

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            var filterAltKeyToPreventFocusLoss = (m.Msg == WM_SYSKEYDOWN || m.Msg == WM_SYSKEYUP) && (int)m.WParam == VK_ALT;
            if (!filterAltKeyToPreventFocusLoss)
                base.WndProc(ref m);

            ImGuiIOPtr io = ImGui.GetIO();
            switch (m.Msg)
            {
                case WM_LBUTTONDOWN:
                case WM_LBUTTONDBLCLK:
                case WM_RBUTTONDOWN:
                case WM_RBUTTONDBLCLK:
                case WM_MBUTTONDOWN:
                case WM_MBUTTONDBLCLK:
                {
                    int button = 0;
                    if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK) button = 0;
                    if (m.Msg == WM_RBUTTONDOWN || m.Msg == WM_RBUTTONDBLCLK) button = 1;
                    if (m.Msg == WM_MBUTTONDOWN || m.Msg == WM_MBUTTONDBLCLK) button = 2;
                    // TODO
                    //if (!ImGui.IsAnyMouseDown() && ::GetCapture() == NULL)
                    //    ::SetCapture(hwnd);
                    io.MouseDown[button] = true;
                    return;
                }
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                case WM_MBUTTONUP:
                {
                    int button = 0;
                    if (m.Msg == WM_LBUTTONUP) button = 0;
                    if (m.Msg == WM_RBUTTONUP) button = 1;
                    if (m.Msg == WM_MBUTTONUP) button = 2;
                    io.MouseDown[button] = false;
                    // TODO
                    //if (!ImGui::IsAnyMouseDown() && ::GetCapture() == hwnd)
                    //    ::ReleaseCapture();
                    return;
                }
                case WM_MOUSEWHEEL:
                    io.MouseWheel += (short)(((uint)(long)m.WParam >> 16) & 0xffff) / 120.0f; // TODO (float)WHEEL_DELTA;
                    return;
                case WM_MOUSEHWHEEL:
                    io.MouseWheelH += (short)(((uint)(long)m.WParam >> 16) & 0xffff) / 120.0f; // TODO (float)WHEEL_DELTA;
                    return;
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    switch ((int)m.WParam)
                    {
                        case VK_SHIFT:
                            io.KeyShift = true;
                            break;
                        case VK_CONTROL:
                            io.KeyCtrl = true;
                            break;
                        case VK_ALT:
                            io.KeyAlt = true;
                            break;
                        default:
                        {
                            if ((int)m.WParam < 256)
                                io.KeysDown[(int)m.WParam] = true;
                            break;
                        }
                    }

                    return;
                case WM_KEYUP:
                case WM_SYSKEYUP:
                    switch ((int)m.WParam)
                    {
                        case VK_SHIFT:
                            io.KeyShift = false;
                            break;
                        case VK_CONTROL:
                            io.KeyCtrl = false;
                            break;
                        case VK_ALT:
                            io.KeyAlt = false;
                            break;
                        default:
                        {
                            if ((int)m.WParam < 256)
                                io.KeysDown[(int)m.WParam] = false;
                            break;
                        }
                    }

                    return;
                case WM_CHAR:
                    // You can also use ToAscii()+GetKeyboardState() to retrieve characters.
                    if ((int)m.WParam > 0 && (int)m.WParam < 0x10000)
                        io.AddInputCharacter((ushort)m.WParam);
                    return;
                case WM_SETCURSOR:
                    if ((((int)m.LParam & 0xFFFF) == 1) && UpdateMouseCursor())
                        m.Result = (IntPtr)1;
                    return;
                case WM_SETFOCUS:
                    for (int i = 0; i < io.KeysDown.Count; i++)
                        io.KeysDown[i] = false;
                    io.KeyShift = false;
                    io.KeyCtrl = false;
                    io.KeyAlt = false;
                    break;
            }
        }

        private static bool UpdateMouseCursor()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (((uint)io.ConfigFlags & (uint)ImGuiConfigFlags.NoMouseCursorChange) > 0)
                return false;

            ImGuiMouseCursor imgui_cursor = ImGui.GetMouseCursor();
            if (imgui_cursor == ImGuiMouseCursor.None || io.MouseDrawCursor)
            {
                // Hide OS mouse cursor if imgui is drawing it or if it wants no cursor
                Cursor.Current = null;
            }
            else
            {
                // Show OS mouse cursor
                switch (imgui_cursor)
                {
                    case ImGuiMouseCursor.Arrow:
                        Cursor.Current = Cursors.Arrow;
                        break;
                    case ImGuiMouseCursor.TextInput:
                        Cursor.Current = Cursors.IBeam;
                        break;
                    case ImGuiMouseCursor.ResizeAll:
                        Cursor.Current = Cursors.SizeAll;
                        break;
                    case ImGuiMouseCursor.ResizeEW:
                        Cursor.Current = Cursors.SizeWE;
                        break;
                    case ImGuiMouseCursor.ResizeNS:
                        Cursor.Current = Cursors.SizeNS;
                        break;
                    case ImGuiMouseCursor.ResizeNESW:
                        Cursor.Current = Cursors.SizeNESW;
                        break;
                    case ImGuiMouseCursor.ResizeNWSE:
                        Cursor.Current = Cursors.SizeNWSE;
                        break;
                    case ImGuiMouseCursor.Hand:
                        Cursor.Current = Cursors.Hand;
                        break;
                }
            }

            return true;
        }
    }

    
    public class Program
    {
        private static ImGuiDx11Impl _controller;
        public static Device Device { get; private set; }
        public static bool IsFullScreen { get; set; } = false;

        [STAThread]
        private static void Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            ;

            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();

            var form = new ImGuiDx11RenderForm("T3 ImGui Test")
                           {
                               ClientSize = new Size(1920, 1080),
                               Icon = new Icon(@"Resources\t3\t3.ico", 48, 48) //, 256, 256)
                           };

            // SwapChain description
            var swapChainDescription = new SwapChainDescription()
                           {
                               BufferCount = 3,
                               ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                                                     new Rational(60, 1), Format.R8G8B8A8_UNorm),
                               IsWindowed = true,
                               OutputHandle = form.Handle,
                               SampleDescription = new SampleDescription(1, 0),
                               SwapEffect = SwapEffect.Discard,
                               Usage = Usage.RenderTargetOutput
                           };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, swapChainDescription, out var device, out _mainWindow.SwapChain);
            var context = device.ImmediateContext;
            Device = device;

            // Ignore all windows events
            Factory factory = _mainWindow.SwapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            _mainWindow.BackBufferTexture = Texture2D.FromSwapChain<Texture2D>(_mainWindow.SwapChain, 0);
            _mainWindow.RenderTargetView = new RenderTargetView(device, _mainWindow.BackBufferTexture);

            _controller = new ImGuiDx11Impl(device, form.Width, form.Height);

            form.KeyDown += HandleKeyDown;
            form.KeyUp += HandleKeyUp;

            form.ResizeBegin += (sender, args) => _mainWindow.isResizingRightNow = true;
            form.ResizeEnd += (sender, args) =>
                              {
                                  RebuildBackBuffer(form, device, ref _mainWindow.RenderTargetView, ref _mainWindow.BackBufferTexture, ref _mainWindow.SwapChain);
                                  _mainWindow.isResizingRightNow = false;
                              };
            form.ClientSizeChanged += (sender, args) =>
                                      {
                                          if (_mainWindow.isResizingRightNow)
                                              return;

                                          RebuildBackBuffer(form, device, ref _mainWindow.RenderTargetView, ref _mainWindow.BackBufferTexture, ref _mainWindow.SwapChain);
                                      };
            form.Closing += (sender, args) =>
                            {
                                args.Cancel = T3Ui.UiModel.IsSaving;
                                Log.Debug($"Cancel closing because save-operation is in progress.");
                            };

            form.WindowState = FormWindowState.Maximized;
            _viewerWindow.Initialize("t3 Viewer Window ", factory, device, true);
            _viewerWindow.RenderForm.Show();

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();

            _t3ui = new T3Ui();

            //resourceManager.CreateVertexShader(@"Resources\\vs-fullscreen-tri-pos-only.hlsl", "main", "vs-fullscreen-tri-pos-only");
            //resourceManager.CreatePixelShader(@"Resources\\ps-pos-only-fixed-color.hlsl", "main", "ps-pos-only-fixed-color");
            var di = new DirectoryInfo(".");
            Console.WriteLine(di.FullName);
            FullScreenVertexShaderId =
                resourceManager.CreateVertexShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "vsMain", "vs-fullscreen-texture", () => { });
            FullScreenPixelShaderId =
                resourceManager.CreatePixelShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "psMain", "ps-fullscreen-texture", () => { });
            (uint texId, uint srvId) = resourceManager.CreateTextureFromFile(@"Resources\images\chipmunk.jpg", null);

            // setup file watching the operator source
            resourceManager.OperatorsAssembly = T3Ui.UiModel.OperatorsAssembly;
            foreach (var (_, symbol) in SymbolRegistry.Entries)
            {
                ResourceManager.Instance().CreateOperatorEntry(@"Operators\Types\" + symbol.Name + ".cs", symbol.Id.ToString(), OperatorUpdating.Update);
            }

            Console.WriteLine($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");
            ShaderResourceView backgroundSrv = null;

            unsafe
            {
                // disable imgui ini file settings
                ImGui.GetIO().NativePtr->IniFilename = null;
            }

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            startupStopWatch.Stop();
            Log.Debug($"startup took {startupStopWatch.ElapsedMilliseconds}ms.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Int64 lastElapsedTicks = stopwatch.ElapsedTicks;

            // Main loop
            RenderLoop.Run(form, () =>
                                 {
                                     Int64 ticks = stopwatch.ElapsedTicks;
                                     Int64 ticksDiff = ticks - lastElapsedTicks;
                                     ImGui.GetIO().DeltaTime = (float)((double)(ticksDiff) / Stopwatch.Frequency);
                                     lastElapsedTicks = ticks;
                                     ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(form.ClientSize.Width, form.ClientSize.Height);

                                     // Toggling full screen
                                     bool isFullScreenBorderStyle = form.FormBorderStyle == FormBorderStyle.None;
                                     if (isFullScreenBorderStyle != IsFullScreen)
                                     {
                                         if (IsFullScreen)
                                         {
                                             form.FormBorderStyle = FormBorderStyle.Sizable;
                                             form.WindowState = FormWindowState.Normal;
                                             form.FormBorderStyle = FormBorderStyle.None;
                                             var screenIndexForMainScreen = UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen ? 1: 0;
                                             var screenIndexFor2ndScreen = UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen ? 0: 1;;
                                             form.Bounds = Screen.AllScreens[screenIndexForMainScreen].Bounds;

                                             if (T3Ui.ShowSecondaryRenderWindow)
                                             {
                                                 _viewerWindow.RenderForm.WindowState = FormWindowState.Normal;
                                                 _viewerWindow.RenderForm.FormBorderStyle = FormBorderStyle.None;
                                                 _viewerWindow.RenderForm.Bounds = Screen.AllScreens[screenIndexFor2ndScreen].Bounds;
                                             }
                                             else
                                             {
                                                 _viewerWindow.RenderForm.WindowState = FormWindowState.Normal;
                                                 _viewerWindow.RenderForm.FormBorderStyle = FormBorderStyle.None;
                                                 _viewerWindow.RenderForm.Bounds = Screen.AllScreens[screenIndexForMainScreen].Bounds;
                                             }
                                         }
                                         else
                                         {
                                             form.FormBorderStyle = FormBorderStyle.Sizable;
                                             _viewerWindow.RenderForm.FormBorderStyle = FormBorderStyle.Sizable;
                                         }
                                         //form.FormBorderStyle = isFullScreenBorderStyle ? FormBorderStyle.Sizable : FormBorderStyle.None;
                                         //_viewerWindow.RenderForm.FormBorderStyle = fullScreenBorderStyle ? FormBorderStyle.Sizable : FormBorderStyle.None;
                                     }

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
                                     context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                                     context.OutputMerger.SetTargets(_mainWindow.RenderTargetView);
                                     context.ClearRenderTargetView(_mainWindow.RenderTargetView, T3Style.Colors.WindowBackground.AsSharpDx);

                                     // Render 2nd view
                                     _viewerWindow.RenderForm.Visible = T3Ui.ShowSecondaryRenderWindow;
                                     if (T3Ui.ShowSecondaryRenderWindow)
                                     {
                                         context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                                         context.Rasterizer.SetViewport(new Viewport(0, 0, _viewerWindow.RenderForm.ClientSize.Width, _viewerWindow.RenderForm.ClientSize.Height, 0.0f, 1.0f));
                                         context.OutputMerger.SetTargets(_viewerWindow.RenderTargetView);
                                         context.ClearRenderTargetView(_viewerWindow.RenderTargetView, T3Style.Colors.WindowBackground.AsSharpDx);

                                         if (resourceManager.Resources[FullScreenVertexShaderId] is VertexShaderResource vsr)
                                             context.VertexShader.Set(vsr.VertexShader);
                                         
                                         if (resourceManager.Resources[FullScreenPixelShaderId] is PixelShaderResource psr)
                                             context.PixelShader.Set(psr.PixelShader);

                                         if (resourceManager.SecondRenderWindowTexture != null && !resourceManager.SecondRenderWindowTexture.IsDisposed)
                                         {
                                             if (backgroundSrv == null || backgroundSrv.Resource.NativePointer !=
                                                 resourceManager.SecondRenderWindowTexture.NativePointer)
                                             {
                                                 backgroundSrv?.Dispose();
                                                 backgroundSrv = new ShaderResourceView(device, resourceManager.SecondRenderWindowTexture);
                                             }

                                             context.PixelShader.SetShaderResource(0, backgroundSrv);
                                         }
                                         else if (resourceManager.Resources[srvId] is ShaderResourceViewResource srvr)
                                             context.PixelShader.SetShaderResource(0, srvr.ShaderResourceView);

                                         context.Draw(3, 0);
                                         context.PixelShader.SetShaderResource(0, null);
                                     }

                                     _t3ui.Draw();

                                     context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                                     context.OutputMerger.SetTargets(_mainWindow.RenderTargetView);

                                     ImGui.Render();
                                     _controller.RenderImDrawData(ImGui.GetDrawData());

                                     T3Metrics.UiRenderingCompleted();

                                     _mainWindow.SwapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);

                                     if (T3Ui.ShowSecondaryRenderWindow)
                                         _viewerWindow.SwapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);
                                 });

            try
            {
                _controller.Dispose();
            }
            catch(Exception e)
            {
                Log.Warning("Exception during shutdown: " + e);
            }

            // Release all resources
            _mainWindow.RenderTargetView.Dispose();
            _mainWindow.BackBufferTexture.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            _mainWindow.SwapChain.Dispose();
            factory.Dispose();
        }

        private static void RebuildBackBuffer(ImGuiDx11RenderForm form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, ref SwapChain swapChain)
        {
            rtv.Dispose();
            buffer.Dispose();
            swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, 0);
            buffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            rtv = new RenderTargetView(device, buffer);
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


 
        
        private class WindowParameters
        {
            public SwapChain SwapChain;
            public RenderTargetView RenderTargetView;
            public Texture2D BackBufferTexture;
            public bool isResizingRightNow;
            public ImGuiDx11RenderForm RenderForm;


            
            
            public void Initialize(string windowTitle, Factory factory, Device device, bool disableClose)
            {
                RenderForm = disableClose ?  new NoCloseRenderForm(windowTitle) { ClientSize = new Size(640, 480) }: 
                                 new ImGuiDx11RenderForm(windowTitle) { ClientSize = new Size(640, 480) };
                var swapChainDescription = new SwapChainDescription()
                                               {
                                                   BufferCount = 3,
                                                   ModeDescription = new ModeDescription(RenderForm.ClientSize.Width, RenderForm.ClientSize.Height,
                                                                                         new Rational(60, 1), Format.R8G8B8A8_UNorm),
                                                   IsWindowed = true,
                                                   OutputHandle = RenderForm.Handle,
                                                   SampleDescription = new SampleDescription(1, 0),
                                                   SwapEffect = SwapEffect.Discard,
                                                   Usage = Usage.RenderTargetOutput
                                               };
                
                SwapChain = new SwapChain(factory, device, swapChainDescription);

                SwapChain.ResizeBuffers(bufferCount: 3, RenderForm.ClientSize.Width, RenderForm.ClientSize.Height,
                                        SwapChain.Description.ModeDescription.Format, SwapChain.Description.Flags);
                
                BackBufferTexture = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
                RenderTargetView = new RenderTargetView(device, BackBufferTexture);

                RenderForm.ResizeBegin += (sender, args) => isResizingRightNow = true;
                RenderForm.ResizeEnd += (sender, args) =>
                                   {
                                       RebuildBackBuffer(RenderForm, device, ref RenderTargetView, ref BackBufferTexture, ref SwapChain);
                                       isResizingRightNow = false;
                                   };
                RenderForm.ClientSizeChanged += (sender, args) =>
                                           {
                                               if (isResizingRightNow)
                                                   return;

                                               RebuildBackBuffer(RenderForm, device, ref RenderTargetView, ref BackBufferTexture, ref SwapChain);
                                           };                
            }            

            private class NoCloseRenderForm : ImGuiDx11RenderForm
            {
                private const int CP_NOCLOSE_BUTTON = 0x200;
            
                protected override CreateParams CreateParams
                {
                    get
                    {
                        CreateParams myCp = base.CreateParams;
                        myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON ;
                        return myCp;
                    }
                }

                public NoCloseRenderForm(string title) : base(title)
                {
                }
            }
            
        }

        private static WindowParameters _mainWindow = new WindowParameters();
        private static WindowParameters _viewerWindow  = new WindowParameters();
        
        private static T3Ui _t3ui = null;
        // private static bool _inResize;
        // private static bool _inResize2;
        // private static SwapChain _swapChain;
        // private static SwapChain _swapChain2;
        // private static RenderTargetView _mainWindowRtv;
        // private static Texture2D _mainWindowBackBuffer;
        // private static Texture2D _secondaryWindowBackBuffer;
        // private static RenderTargetView _secondaryWindowRtv;
        public static uint FullScreenVertexShaderId { get; private set; }
        public static uint FullScreenPixelShaderId { get; private set; }
    }
}