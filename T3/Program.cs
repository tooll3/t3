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
using T3.Core;
using T3.Core.Operator;
using T3.Gui;
using T3.Logging;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;

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
                    if (((int)m.WParam) < 256)
                        io.KeysDown[(int)m.WParam] = true;
                    return;
                case WM_KEYUP:
                case WM_SYSKEYUP:
                    if ((int)m.WParam < 256)
                        io.KeysDown[(int)m.WParam] = false;
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
                    case ImGuiMouseCursor.Arrow: Cursor.Current = Cursors.Arrow; break;
                    case ImGuiMouseCursor.TextInput: Cursor.Current = Cursors.IBeam; break;
                    case ImGuiMouseCursor.ResizeAll: Cursor.Current = Cursors.SizeAll; break;
                    case ImGuiMouseCursor.ResizeEW: Cursor.Current = Cursors.SizeWE; break;
                    case ImGuiMouseCursor.ResizeNS: Cursor.Current = Cursors.SizeNS; break;
                    case ImGuiMouseCursor.ResizeNESW: Cursor.Current = Cursors.SizeNESW; break;
                    case ImGuiMouseCursor.ResizeNWSE: Cursor.Current = Cursors.SizeNWSE; break;
                    case ImGuiMouseCursor.Hand: Cursor.Current = Cursors.Hand; break;
                }
            }
            return true;
        }
    }

    public class Program
    {
        private static ImGuiDx11Impl _controller;

        [STAThread]
        private static void Main()
        {
            var form = new ImGuiDx11RenderForm("T3 ImGui Test") { ClientSize = new Size(1920, 1080) };

            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                                                 new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            Device device;
            SwapChain swapChain;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out device, out swapChain);
            var context = device.ImmediateContext;

            // Ignore all windows events
            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderView = new RenderTargetView(device, backBuffer);

            // Prepare All the stages
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
            context.OutputMerger.SetTargets(renderView);

            _controller = new ImGuiDx11Impl(device, form.Width, form.Height);

            form.ResizeEnd += (sender, args) =>
                              {
                                  renderView.Dispose();
                                  backBuffer.Dispose();
                                  swapChain.ResizeBuffers(0, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, 0);
                                  backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                                  renderView = new RenderTargetView(device, backBuffer);
                              };

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            //resourceManager.CreateVertexShader(@"Resources\\vs-fullscreen-tri-pos-only.hlsl", "main", "vs-fullscreen-tri-pos-only");
            //resourceManager.CreatePixelShader(@"Resources\\ps-pos-only-fixed-color.hlsl", "main", "ps-pos-only-fixed-color");
            var di = new DirectoryInfo(".");
            System.Console.WriteLine(di.FullName);
            Guid vsId = resourceManager.CreateVertexShader(@"Resources\fullscreen-texture.hlsl", "vsMain", "vs-fullscreen-texture");
            Guid psId = resourceManager.CreatePixelShader(@"Resources\\fullscreen-texture.hlsl", "psMain", "ps-fullscreen-texture");
            (Guid texId, Guid srvId) = resourceManager.CreateTextureFromFile(@"Resources\chipmunk.jpg");
            var addId = resourceManager.CreateOperatorEntry(@"..\Core\Operator\Types\Add.cs", "Add");
            Console.WriteLine($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");


            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            // Main loop
            RenderLoop.Run(form, () =>
                                 {
                                     Int64 ticks = stopwatch.ElapsedTicks;
                                     ImGui.GetIO().DeltaTime = (float)(ticks) / Stopwatch.Frequency;
                                     ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(form.ClientSize.Width, form.ClientSize.Height);
                                     stopwatch.Restart();

                                     if (resourceManager.Resources[addId] is OperatorResource opResource)
                                     {
                                         if (opResource.Updated)
                                         {
                                             Type type = opResource.OperatorAssembly.ExportedTypes.First();
                                             var addSymbol = SymbolRegistry.Entries.First(e => e.Value.SymbolName == "Add").Value;
                                             addSymbol.SetInstanceType(type);
                                             opResource.Updated = false;
                                             Log.Info($"type updating took: {(double)stopwatch.ElapsedTicks/Stopwatch.Frequency}s");
                                         }
                                     }

                                     Metrics.UiRenderingStarted();
                                     _t3ui.InitStyle();

                                     ImGui.NewFrame();

                                     context.OutputMerger.SetTargets(renderView);
                                     context.ClearRenderTargetView(renderView, new Color(0.45f, 0.55f, 0.6f, 1.0f));

                                     if (resourceManager.Resources[vsId] is VertexShaderResource vsr)
                                         context.VertexShader.Set(vsr.VertexShader);
                                     if (resourceManager.Resources[psId] is PixelShaderResource psr)
                                         context.PixelShader.Set(psr.PixelShader);
                                     if (resourceManager.Resources[srvId] is ShaderResourceViewResource srvr)
                                         context.PixelShader.SetShaderResource(0, srvr.ShaderResourceView);
                                     context.Draw(3, 0);

                                     _t3ui.DrawUI();
                                     _t3ui.DrawSelectionParameters();
                                     _t3ui.DrawSelectedOutput();

                                     UiSettingsWindow.DrawUiSettings();

                                     ImGui.Render();
                                     _controller.RenderImDrawData(ImGui.GetDrawData());

                                     Metrics.UiRenderingCompleted();

                                     swapChain.Present(UiSettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);
                                 });

            _controller.Dispose();

            // Release all resources
            renderView.Dispose();
            backBuffer.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            swapChain.Dispose();
            factory.Dispose();
        }

        private static T3UI _t3ui = new T3UI();
    }
}
