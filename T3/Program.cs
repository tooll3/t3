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
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace T3
{
    public class ImGuiDx11RenderForm : RenderForm
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
            
            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();
            
            _main.CreateRenderForm("T3", false);

            // _main.Form = new ImGuiDx11RenderForm("T3 ImGui Test")
            //                {
            //                    ClientSize = new Size(1920, 1080),
            //                    Icon = new Icon(@"Resources\t3\t3.ico", 48, 48) //, 256, 256)
            //                };
            //
            // // SwapChain description
            // var swapChainDescription = new SwapChainDescription()
            //                {
            //                    BufferCount = 3,
            //                    ModeDescription = new ModeDescription(_main.Form.ClientSize.Width, _main.Form.ClientSize.Height,
            //                                                          new Rational(60, 1), Format.R8G8B8A8_UNorm),
            //                    IsWindowed = true,
            //                    OutputHandle = _main.Form.Handle,
            //                    SampleDescription = new SampleDescription(1, 0),
            //                    SwapEffect = SwapEffect.Discard,
            //                    Usage = Usage.RenderTargetOutput
            //                };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, _main.SwapChainDescription, out var device, out _main.SwapChain);
            _deviceContext = device.ImmediateContext;
            Device = device;
            Factory factory = _main.SwapChain.GetParent<Factory>();

            // Ignore all windows events
            factory.MakeWindowAssociation(_main.Form.Handle, WindowAssociationFlags.IgnoreAll);
            
            _main.InitRenderTargetsAndEventHandlers(device);

            _controller = new ImGuiDx11Impl(device, _main.Form.Width, _main.Form.Height);
            
            _main.Form.KeyDown += HandleKeyDown;
            _main.Form.KeyUp += HandleKeyUp;
            _main.Form.Closing += (sender, args) =>
                            {
                                args.Cancel = T3Ui.UiModel.IsSaving;
                                Log.Debug($"Cancel closing because save-operation is in progress.");
                            };

            _main.Form.WindowState = FormWindowState.Maximized;
            
            _viewer.CreateRenderForm("T3 Viewer", true);
            _viewer.InitViewSwapChain(factory,device);
            _viewer.InitRenderTargetsAndEventHandlers(device);
            _viewer.Form.Show();

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
                // disable ImGui ini file settings
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

                    if (resourceManager.Resources[FullScreenVertexShaderId] is VertexShaderResource vsr)
                        _deviceContext.VertexShader.Set(vsr.VertexShader);

                    if (resourceManager.Resources[FullScreenPixelShaderId] is PixelShaderResource psr)
                        _deviceContext.PixelShader.Set(psr.PixelShader);

                    if (resourceManager.SecondRenderWindowTexture != null && !resourceManager.SecondRenderWindowTexture.IsDisposed)
                    {
                        if (backgroundSrv == null || backgroundSrv.Resource.NativePointer != resourceManager.SecondRenderWindowTexture.NativePointer)
                        {
                            backgroundSrv?.Dispose();
                            backgroundSrv = new ShaderResourceView(device, resourceManager.SecondRenderWindowTexture);
                        }

                        _deviceContext.PixelShader.SetShaderResource(0, backgroundSrv);
                    }
                    else if (resourceManager.Resources[srvId] is ShaderResourceViewResource srvr)
                        _deviceContext.PixelShader.SetShaderResource(0, srvr.ShaderResourceView);

                    _deviceContext.Draw(3, 0);
                    _deviceContext.PixelShader.SetShaderResource(0, null);
                }

                _t3ui.Draw();

                _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, _main.Form.ClientSize.Width, _main.Form.ClientSize.Height, 0.0f, 1.0f));
                _deviceContext.OutputMerger.SetTargets(_main.RenderTargetView);

                ImGui.Render();
                _controller.RenderImDrawData(ImGui.GetDrawData());

                T3Metrics.UiRenderingCompleted();

                _main.SwapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);

                if (T3Ui.ShowSecondaryRenderWindow)
                    _viewer.SwapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);
            }

            RenderLoop.Run(_main.Form, RenderCallback);

            try
            {
                _controller.Dispose();
            }
            catch(Exception e)
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
            bool isFullScreenBorderStyle = _main.Form.FormBorderStyle == FormBorderStyle.None;
            if (isFullScreenBorderStyle != IsFullScreen)
            {
                if (IsFullScreen)
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
        private static readonly AppWindow _viewer  = new();
        
        private static T3Ui _t3ui = null;
        private static DeviceContext _deviceContext;

        public static uint FullScreenVertexShaderId { get; private set; }
        public static uint FullScreenPixelShaderId { get; private set; }
    }
    

        
        public class AppWindow
        {
            public SwapChain SwapChain;
            public RenderTargetView RenderTargetView;
            public Texture2D BackBufferTexture;
            public bool isResizingRightNow;
            public ImGuiDx11RenderForm Form;
            public SwapChainDescription SwapChainDescription;
            
            public void CreateRenderForm(string windowTitle, bool disableClose)
            {
                Form = disableClose
                             ? new NoCloseRenderForm(windowTitle)
                                   {
                                       ClientSize = new Size(640, 480),
                                       Icon = new Icon(@"Resources\t3\t3.ico", 48, 48)
                                   }
                             : new ImGuiDx11RenderForm(windowTitle)
                                   {
                                       ClientSize = new Size(640, 480),
                                       Icon = new Icon(@"Resources\t3\t3.ico", 48, 48)
                                   };

                SwapChainDescription = new SwapChainDescription()
                                               {
                                                   BufferCount = 3,
                                                   ModeDescription = new ModeDescription(Form.ClientSize.Width,
                                                                                         Form.ClientSize.Height,
                                                                                         new Rational(60, 1),
                                                                                         Format.R8G8B8A8_UNorm),
                                                   IsWindowed = true,
                                                   OutputHandle = Form.Handle,
                                                   SampleDescription = new SampleDescription(1, 0),
                                                   SwapEffect = SwapEffect.Discard,
                                                   Usage = Usage.RenderTargetOutput
                                               };
            }

            public void InitViewSwapChain(Factory factory, Device device)
            {
                SwapChain = new SwapChain(factory, device, SwapChainDescription);
                SwapChain.ResizeBuffers(bufferCount: 3,Form.ClientSize.Width, Form.ClientSize.Height,
                                                SwapChain.Description.ModeDescription.Format, SwapChain.Description.Flags);

            }
            
            public void InitRenderTargetsAndEventHandlers(Device device)
            {
                
                BackBufferTexture = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
                RenderTargetView = new RenderTargetView(device, BackBufferTexture);

                Form.ResizeBegin += (sender, args) => isResizingRightNow = true;
                Form.ResizeEnd += (sender, args) =>
                                   {
                                       RebuildBackBuffer(Form, device, ref RenderTargetView, ref BackBufferTexture, ref SwapChain);
                                       isResizingRightNow = false;
                                   };
                Form.ClientSizeChanged += (sender, args) =>
                                           {
                                               if (isResizingRightNow)
                                                   return;

                                               RebuildBackBuffer(Form, device, ref RenderTargetView, ref BackBufferTexture, ref SwapChain);
                                           };                
            }

            public void PrepareRenderingFrame(DeviceContext deviceContext)
            {
                deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, Form.ClientSize.Width, Form.ClientSize.Height, 0.0f, 1.0f));
                deviceContext.OutputMerger.SetTargets(RenderTargetView);
                deviceContext.ClearRenderTargetView(RenderTargetView, T3Style.Colors.WindowBackground.AsSharpDx);                
            }



            /// <summary>
            /// We prevent closing the secondary viewer window for now because
            /// this will cause a SwapChain related crash
            /// </summary>
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
            
            
            private static void RebuildBackBuffer(ImGuiDx11RenderForm form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, ref SwapChain swapChain)
            {
                rtv.Dispose();
                buffer.Dispose();
                swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, 0);
                buffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                rtv = new RenderTargetView(device, buffer);
            }
        }    
}