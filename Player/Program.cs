using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;

namespace T3
{
    public class Program
    {
        public static Device Device { get; private set; }

        [STAThread]
        private static void Main()
        {
            var form = new RenderForm("T3-Player") { ClientSize = new Size(1920, 1080) };

            // SwapChain description
            var desc = new SwapChainDescription()
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
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out var device, out _swapChain);
            var context = device.ImmediateContext;
            Device = device;

            // Ignore all windows events
            var factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
            
            form.KeyUp += (sender, args) =>
                          {
                              if (args.Alt && args.KeyCode == Keys.Enter)
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

                              if (args.KeyCode == Keys.Escape)
                              {
                                  Application.Exit();
                              }
                          };

            // New RenderTargetView from the backbuffer
            _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
            _renderView = new RenderTargetView(device, _backBuffer);

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

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            var di = new DirectoryInfo(".");
            Console.WriteLine(di.FullName);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Main loop
            RenderLoop.Run(form, () =>
                                 {
                                     Int64 ticks = stopwatch.ElapsedTicks;
                                     // Console.WriteLine("{0}", (double)ticks/Stopwatch.Frequency);
                                     stopwatch.Restart();
                                     DirtyFlag.IncrementGlobalTicks();

                                     context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                                     context.OutputMerger.SetTargets(_renderView);
                                     
                                     // todo: remove this and add operator evaluation here:
                                     context.ClearRenderTargetView(_renderView, new Color(0.45f, 0.55f, 0.6f, 1.0f));

                                     
                                     // _swapChain.Present(SettingsWindow.UseVSync ? 1 : 0, PresentFlags.None);
                                     _swapChain.Present(1, PresentFlags.None);
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

        private static void RebuildBackBuffer(RenderForm form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, ref SwapChain swapChain)
        {
            rtv.Dispose();
            buffer.Dispose();
            swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, 0);
            buffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            rtv = new RenderTargetView(device, buffer);
        }

        private static bool _inResize;
        private static SwapChain _swapChain;
        private static RenderTargetView _renderView;
        private static Texture2D _backBuffer;
    }
}