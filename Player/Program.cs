using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CommandLine;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;

namespace T3
{
    public class Program
    {
        public static Device Device { get; private set; }

        public class Options
        {
            [Option('v', "no-vsync", Required = false, HelpText = "Disables vsync")]
            public bool Vsync { get; set; }

            [Option('w', "width", Default = 1920, Required = false, HelpText = "Defines the width")]
            public int Width { get; set; }

            [Option('h', "height", Default = 1080, Required = false, HelpText = "Defines the height")]
            public int Height { get; set; }

            [Option('f', "fullscreen", Required = false, HelpText = "Run in fullscreen mode")]
            public bool Fullscreen { get; set; }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            bool isWindowed = false;
            bool exit = false;
            Size size = new Size(1920, 1080);

            var parser = new Parser(config => config.HelpWriter = Console.Out);
            parser.ParseArguments<Options>(args)
                  .WithParsed(o =>
                              {
                                  _vsync = !o.Vsync;
                                  isWindowed = !o.Fullscreen;
                                  size = new Size(o.Width, o.Height);
                                  Console.WriteLine($"using vsync: {_vsync}, windowed: {isWindowed}, size: {size}");
                              })
                  .WithNotParsed(o => exit = true);

            if (exit)
                return;

            var form = new RenderForm("T3-Player") { ClientSize = size };
            form.AllowUserResizing = false;

            // SwapChain description
            var desc = new SwapChainDescription()
                       {
                           BufferCount = 3,
                           ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                                                 new Rational(60, 1), Format.R8G8B8A8_UNorm),
                           IsWindowed = isWindowed,
                           OutputHandle = form.Handle,
                           SampleDescription = new SampleDescription(1, 0),
                           SwapEffect = SwapEffect.Discard,
                           Usage = Usage.RenderTargetOutput
                       };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out var device, out _swapChain);
            var context = device.ImmediateContext;
            Device = device;

            // Ignore all windows events
            var factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
            
            form.KeyUp += (sender, keyArgs) =>
                          {
                              if (keyArgs.Alt && keyArgs.KeyCode == Keys.Enter)
                              {
                                  _swapChain.IsFullScreen = !_swapChain.IsFullScreen;
                                  if (_swapChain.IsFullScreen)
                                  {
                                      Cursor.Hide();
                                  }
                                  else
                                  {
                                      Cursor.Show();
                                  }
                              }

                              if (keyArgs.KeyCode == Keys.Escape)
                              {
                                  Application.Exit();
                              }
                          };

            // New RenderTargetView from the backbuffer
            _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
            _renderView = new RenderTargetView(device, _backBuffer);

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            FullScreenVertexShaderId = resourceManager.CreateVertexShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "vsMain", "vs-fullscreen-texture", () => { });
            FullScreenPixelShaderId = resourceManager.CreatePixelShaderFromFile(@"Resources\lib\dx11\fullscreen-texture.hlsl", "psMain", "ps-fullscreen-texture", () => { });
            _model = new Model();
            _model.Load();
            
            var symbols = SymbolRegistry.Entries;
            var demoSymbol = symbols.First(entry => entry.Value.Name == "Demo").Value;
            // create instance of project op, all children are create automatically
            _project = demoSymbol.CreateInstance(Guid.NewGuid());
            _evalContext = new EvaluationContext();
            _playback = new StreamPlayback("Resources\\proj-partial\\soundtrack\\technotoad-02.mp3");
            _playback.PlaybackSpeed = 1.0;

            var stopwatch = new Stopwatch();
            stopwatch.Start();


            // Main loop
            RenderLoop.Run(form, () =>
                                 {
                                     DirtyFlag.IncrementGlobalTicks();
                                     DirtyFlag.InvalidationRefFrame++;

                                     context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                                     context.OutputMerger.SetTargets(_renderView);
                                     
                                     _evalContext.Reset();
                                     _playback.Update(1.0f);
                                     
                                     if (_project.Outputs[0] is Slot<Texture2D> textureOutput)
                                     {
                                         textureOutput.Invalidate();
                                         Texture2D tex = textureOutput.GetValue(_evalContext);

                                         if (resourceManager.Resources[FullScreenVertexShaderId] is VertexShaderResource vsr)
                                             context.VertexShader.Set(vsr.VertexShader);
                                         if (resourceManager.Resources[FullScreenPixelShaderId] is PixelShaderResource psr)
                                             context.PixelShader.Set(psr.PixelShader);
                                         var srv = new ShaderResourceView(device, tex);
                                         context.PixelShader.SetShaderResource(0, srv);

                                         context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                                         context.ClearRenderTargetView(_renderView, new Color(0.45f, 0.55f, 0.6f, 1.0f));
                                         context.Draw(3, 0);
                                         context.PixelShader.SetShaderResource(0, null);
                                     }
                                     
                                     _swapChain.Present(_vsync ? 1 : 0, PresentFlags.None);
                                 });

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

        // private static bool _inResize;
        private static bool _vsync;
        private static SwapChain _swapChain;
        private static RenderTargetView _renderView;
        private static Texture2D _backBuffer;
        private static Model _model;
        private static Instance _project;
        private static EvaluationContext _evalContext;
        private static Playback _playback;
        public static uint FullScreenVertexShaderId { get; private set; }
        public static uint FullScreenPixelShaderId { get; private set; }
    }
}