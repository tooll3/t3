using System.Drawing;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Gui;
using Device = SharpDX.Direct3D11.Device;

namespace t3.App
{
    /// <summary>
    /// Functions and properties related to rendering DX11 content into  RenderForm windows
    /// </summary>
    public class AppWindow
    {
        public SwapChain SwapChain;
        public RenderTargetView RenderTargetView;
        public Texture2D BackBufferTexture;
        public ImGuiDx11RenderForm Form;
        public SwapChainDescription SwapChainDescription;

        public void CreateRenderForm(string windowTitle, bool disableClose)
        {
            Form = disableClose
                       ? new NoCloseRenderForm(windowTitle)
                             {
                                 ClientSize = new Size(640, 360+20),
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
            SwapChain.ResizeBuffers(bufferCount: 3, Form.ClientSize.Width, Form.ClientSize.Height,
                                    SwapChain.Description.ModeDescription.Format, SwapChain.Description.Flags);
        }

        public void InitRenderTargetsAndEventHandlers(Device device)
        {
            BackBufferTexture = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
            RenderTargetView = new RenderTargetView(device, BackBufferTexture);

            Form.ResizeBegin += (sender, args) => _isResizingRightNow = true;
            Form.ResizeEnd += (sender, args) =>
                              {
                                  RebuildBackBuffer(Form, device, ref RenderTargetView, ref BackBufferTexture, ref SwapChain);
                                  _isResizingRightNow = false;
                              };
            Form.ClientSizeChanged += (sender, args) =>
                                      {
                                          if (_isResizingRightNow)
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
                    myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
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

        private bool _isResizingRightNow;
    }
}