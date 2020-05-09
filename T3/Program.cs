using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
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
                    io.MouseWheel += (short)(((uint)m.WParam >> 16) & 0xffff) / 120.0f; // TODO (float)WHEEL_DELTA;
                    return;
                case WM_MOUSEHWHEEL:
                    io.MouseWheelH += (short)(((uint)m.WParam >> 16) & 0xffff) / 120.0f; // TODO (float)WHEEL_DELTA;
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

        [STAThread]
        private static void Main()
        {
            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();
            
            _t3ui = new T3Ui();

            var form = new ImGuiDx11RenderForm("T3 ImGui Test")
                           {
                               ClientSize = new Size(1920, 1080),
                               Icon = new Icon(@"Resources\t3\t3.ico", 48, 48)//, 256, 256)
                           };

            // SwapChain description
            var desc = new SwapChainDescription()
                       {
                           BufferCount = 3,
                           ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                                                 new Rational(60, 1), Format.R8G8B8A8_UNorm),
                           IsWindowed = true,
                           OutputHandle = form.Handle,
                           SampleDescription = new SampleDescription(1, 0),
                           SwapEffect = SwapEffect.FlipDiscard,
                           Usage = Usage.RenderTargetOutput
                       };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out var device, out _swapChain);
            var context = device.ImmediateContext;
            Device = device;

            // Ignore all windows events
            var factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
            _renderView = new RenderTargetView(device, _backBuffer);

            _controller = new ImGuiDx11Impl(device, form.Width, form.Height);

            form.ResizeBegin += (sender, args) => _inResize = true;
            form.ResizeEnd += (sender, args) =>
                              {
                                  RebuildBackBuffer(form, device, ref _renderView, ref _backBuffer, ref _swapChain);
                                  _inResize = false;
                              };
            form.ClientSizeChanged += (sender, args) =>
                                      {
                                          if (_inResize)
                                              return;

                                          RebuildBackBuffer(form, device, ref _renderView, ref _backBuffer, ref _swapChain);
                                      };
            form.WindowState = FormWindowState.Maximized;

            // second render view
            var form2 = new ImGuiDx11RenderForm("T3 Render Window") { ClientSize = new Size(640, 480) };
            desc.OutputHandle = form2.Handle;

            _swapChain2 = new SwapChain(factory, device, desc);

            _swapChain2.ResizeBuffers(3, form2.ClientSize.Width, form2.ClientSize.Height,
                                      _swapChain2.Description.ModeDescription.Format, _swapChain2.Description.Flags);
            _backBuffer2 = Texture2D.FromSwapChain<Texture2D>(_swapChain2, 0);
            _renderView2 = new RenderTargetView(device, _backBuffer2);

            form2.ResizeBegin += (sender, args) => _inResize2 = true;
            form2.ResizeEnd += (sender, args) =>
                               {
                                   RebuildBackBuffer(form2, device, ref _renderView2, ref _backBuffer2, ref _swapChain2);
                                   _inResize2 = false;
                               };
            form2.ClientSizeChanged += (sender, args) =>
                                       {
                                           if (_inResize2)
                                               return;

                                           RebuildBackBuffer(form2, device, ref _renderView2, ref _backBuffer2, ref _swapChain2);
                                       };
            // form2.WindowState = FormWindowState.Maximized;
            form2.Show();

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            //resourceManager.CreateVertexShader(@"Resources\\vs-fullscreen-tri-pos-only.hlsl", "main", "vs-fullscreen-tri-pos-only");
            //resourceManager.CreatePixelShader(@"Resources\\ps-pos-only-fixed-color.hlsl", "main", "ps-pos-only-fixed-color");
            var di = new DirectoryInfo(".");
            Console.WriteLine(di.FullName);
            FullScreenVertexShaderId = resourceManager.CreateVertexShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "vsMain", "vs-fullscreen-texture", () => { });
            FullScreenPixelShaderId = resourceManager.CreatePixelShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "psMain", "ps-fullscreen-texture", () => { });
            (uint texId, uint srvId) = resourceManager.CreateTextureFromFile(@"Resources\images\chipmunk.jpg", null);

            // setup file watching the operator source
            resourceManager.OperatorsAssembly = T3Ui.UiModel.OperatorsAssembly;
            foreach (var (_, symbol) in SymbolRegistry.Entries)
            {
                ResourceManager.Instance().CreateOperatorEntry(@"Operators\Types\" + symbol.Name + ".cs", symbol.Id.ToString(), OperatorUpdating.Update);
            }

            Console.WriteLine($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");
            ShaderResourceView backgroundSrv = null;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            unsafe
            {
                // disable imgui ini file settings
                ImGui.GetIO().NativePtr->IniFilename = null;
            }
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            startupStopWatch.Stop();
            Log.Debug($"startup took {startupStopWatch.ElapsedMilliseconds}ms.");
            
            //T3Style.Init();

            // Main loop
            RenderLoop.Run(form, () =>
                                 {
                                     Int64 ticks = stopwatch.ElapsedTicks;
                                     ImGui.GetIO().DeltaTime = (float)(ticks) / Stopwatch.Frequency;
                                     ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(form.ClientSize.Width, form.ClientSize.Height);
                                     stopwatch.Restart();

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
                                     context.OutputMerger.SetTargets(_renderView);
                                     context.ClearRenderTargetView(_renderView, new Color(0.45f, 0.55f, 0.6f, 1.0f));

                                     form2.Visible = T3Ui.ShowSecondaryRenderWindow;
                                     if (T3Ui.ShowSecondaryRenderWindow)
                                     {
                                         context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                                         context.Rasterizer.SetViewport(new Viewport(0, 0, form2.ClientSize.Width, form2.ClientSize.Height, 0.0f, 1.0f));
                                         context.OutputMerger.SetTargets(_renderView2);
                                         context.ClearRenderTargetView(_renderView2, new Color(0.45f, 0.55f, 0.6f, 1.0f));

                                         if (resourceManager.Resources[FullScreenVertexShaderId] is VertexShaderResource vsr)
                                             context.VertexShader.Set(vsr.VertexShader);
                                         if (resourceManager.Resources[FullScreenPixelShaderId] is PixelShaderResource psr)
                                             context.PixelShader.Set(psr.PixelShader);
                                         
                                         if (resourceManager.SecondRenderWindowTexture != null && !resourceManager.SecondRenderWindowTexture.IsDisposed)
                                         {
                                             if (backgroundSrv == null || backgroundSrv.Resource.NativePointer != resourceManager.SecondRenderWindowTexture.NativePointer)
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
                                     context.OutputMerger.SetTargets(_renderView);

                                     ImGui.Render();
                                     _controller.RenderImDrawData(ImGui.GetDrawData());

                                     T3Metrics.UiRenderingCompleted();

                                     _swapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);

                                     if (T3Ui.ShowSecondaryRenderWindow)
                                         _swapChain2.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);
                                 });

            _controller.Dispose();

            // Release all resources
            _renderView.Dispose();
            _backBuffer.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            _swapChain.Dispose();
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

        private static T3Ui _t3ui = null;
        private static bool _inResize;
        private static bool _inResize2;
        private static SwapChain _swapChain;
        private static SwapChain _swapChain2;
        private static RenderTargetView _renderView;
        private static Texture2D _backBuffer;
        private static Texture2D _backBuffer2;
        private static RenderTargetView _renderView2;
        public static uint FullScreenVertexShaderId { get; private set; }
        public static uint FullScreenPixelShaderId { get; private set; }
    }
}