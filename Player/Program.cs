using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using ManagedBass;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.Vector;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Vector2 = System.Numerics.Vector2;

namespace T3.Player
{
    internal static class Program
    {
        private class Options
        {
            [Option(Default = false, Required = false, HelpText = "Disable vsync")]
            public bool NoVsync { get; set; }

            [Option(Default = 1920, Required = false, HelpText = "Defines the width")]
            public int Width { get; set; }

            [Option(Default = 1080, Required = false, HelpText = "Defines the height")]
            public int Height { get; set; }

            public Size Size => new(Width, Height);

            [Option(Default = false, Required = false, HelpText = "Run in windowed mode")]
            public bool Windowed { get; set; }

            [Option(Default = false, Required = false, HelpText = "Loops the demo")]
            public bool Loop { get; set; }

            [Option(Default = true, Required = false, HelpText = "Show log messages.")]
            public bool Logging { get; set; }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            var fileWriter = FileWriter.CreateDefault();
            try
            {

                Log.AddWriter(new ConsoleWriter());
                Log.AddWriter(fileWriter);

                var _ = new ProjectSettings(saveOnQuit: false);

                _commandLineOptions = ParseCommandLine(args);
                if (_commandLineOptions == null)
                    return;

                _vsync = !_commandLineOptions.NoVsync;
                Log.Debug($"using vsync: {_vsync}, windowed: {_commandLineOptions.Windowed}, size: {_commandLineOptions.Size}, loop: {_commandLineOptions.Loop}, logging: {_commandLineOptions.Logging}");

                _renderForm = new RenderForm($"{ProjectSettings.Config.MainOperatorName}")
                               {
                                   ClientSize = _commandLineOptions.Size,
                                   AllowUserResizing = false,
                                   Icon = new Icon(@"Resources\t3-editor\images\t3.ico")
                               };

                // SwapChain description
                var desc = new SwapChainDescription()
                               {
                                   BufferCount = 3,
                                   ModeDescription = new ModeDescription(_renderForm.ClientSize.Width, _renderForm.ClientSize.Height,
                                                                         new Rational(60, 1), Format.R8G8B8A8_UNorm),
                                   IsWindowed = _commandLineOptions.Windowed,
                                   OutputHandle = _renderForm.Handle,
                                   SampleDescription = new SampleDescription(1, 0),
                                   SwapEffect = SwapEffect.FlipDiscard,
                                   Flags = SwapChainFlags.AllowModeSwitch,
                                   Usage = Usage.RenderTargetOutput
                               };

                // Create Device and SwapChain
                #if DEBUG || FORCE_D3D_DEBUG
                var deviceCreationFlags = DeviceCreationFlags.Debug;
                #else
                var deviceCreationFlags = DeviceCreationFlags.None;
                #endif
                Device.CreateWithSwapChain(DriverType.Hardware, deviceCreationFlags, desc, out _device, out _swapChain);
                _deviceContext = _device.ImmediateContext;

                if (_swapChain.IsFullScreen)
                {
                    Cursor.Hide();
                }

                // Ignore all windows events
                var factory = _swapChain.GetParent<Factory>();
                factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);

                var startedWindowed = _commandLineOptions.Windowed;

                _renderForm.KeyDown += HandleKeyDown;
                _renderForm.KeyUp += HandleKeyUp;

                _renderForm.KeyUp += (sender, keyArgs) =>
                              {
                                  if (startedWindowed && keyArgs.Alt && keyArgs.KeyCode == Keys.Enter)
                                  {
                                      _swapChain.IsFullScreen = !_swapChain.IsFullScreen;
                                      RebuildBackBuffer(_renderForm, _device, ref _renderView, ref _backBuffer, ref _swapChain);
                                      if (_swapChain.IsFullScreen)
                                      {
                                          Cursor.Hide();
                                      }
                                      else
                                      {
                                          Cursor.Show();
                                      }
                                  }

                                  if (ProjectSettings.Config.EnablePlaybackControlWithKeyboard)
                                  {
                                      switch (keyArgs.KeyCode)
                                      {
                                          case Keys.Left:
                                              Playback.Current.TimeInBars -= 4;
                                              break;
                                          case Keys.Right:
                                              Playback.Current.TimeInBars += 4;
                                              break;
                                          case Keys.Space:
                                              Playback.Current.PlaybackSpeed = Math.Abs(Playback.Current.PlaybackSpeed) > 0.01f ? 0 : 1;
                                              break;
                                      }
                                  }

                                  if (keyArgs.KeyCode == Keys.Escape)
                                  {
                                      Application.Exit();
                                  }
                              };

                _renderForm.MouseMove += MouseMoveHandler;
                _renderForm.MouseClick += MouseMoveHandler;

                // New RenderTargetView from the backbuffer
                _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
                _renderView = new RenderTargetView(_device, _backBuffer);

                var shaderCompiler = new DX11ShaderCompiler
                                         {
                                             Device = _device
                                         };
                ShaderCompiler.Instance = shaderCompiler;
                ResourceManager.Init(_device);
                ResourceManager resourceManager = ResourceManager.Instance();
                var gotVertexShader = resourceManager.TryCreateShaderResource(out  _fullScreenVertexShaderResource,
                                                                              fileName: @"lib\dx11\fullscreen-texture.hlsl",
                                                                              entryPoint: "vsMain",
                                                                              name: "vs-fullscreen-texture",
                                                                              errorMessage: out var errorMessage);
                
                if(!string.IsNullOrWhiteSpace(errorMessage))
                    Log.Error($"Failed to load vertex shader: {errorMessage}");
                    
                
                var gotPixelShader = resourceManager.TryCreateShaderResource(out  _fullScreenPixelShaderResource,
                                                                             fileName: @"lib\dx11\fullscreen-texture.hlsl",
                                                                             entryPoint: "psMain",
                                                                             name: "ps-fullscreen-texture",
                                                                             errorMessage: out errorMessage);
                
                if(!string.IsNullOrWhiteSpace(errorMessage))
                    Log.Error($"Failed to load pixel shader: {errorMessage}");

                Assembly operatorsAssembly;
                try
                {
                    operatorsAssembly = Assembly.LoadFrom("Operators.dll");
                }
                catch (Exception e)
                {
                    Log.Debug($"Error loading operator assembly: '{e.Message}'");
                    return;
                }

                _symbolData = new SymbolData(operatorsAssembly);
                _symbolData.Load(enableLog: false);

                var symbols = SymbolRegistry.Entries;
                var demoSymbol = symbols.First(entry => entry.Value.Name == ProjectSettings.Config.MainOperatorName).Value;

                var playbackSettings = demoSymbol.PlaybackSettings;
                _playback = new Playback
                                {
                                    Settings = playbackSettings
                                };

                // Create instance of project op, all children are create automatically
                _project = demoSymbol.CreateInstance(Guid.NewGuid());
                _evalContext = new EvaluationContext();

                var prerenderRequired = false;

                Bass.Free();
                Bass.Init();

                // Init wasapi input if required
                if (playbackSettings != null && playbackSettings.AudioSource == PlaybackSettings.AudioSources.ProjectSoundTrack)
                {
                    if (playbackSettings.GetMainSoundtrack(out _soundtrack))
                    {
                        if (File.Exists(_soundtrack.FilePath))
                        {
                            _playback.Bpm = _soundtrack.Bpm;
                            // Trigger loading clip
                            AudioEngine.UseAudioClip(_soundtrack, 0);
                            AudioEngine.CompleteFrame(_playback, Playback.LastFrameDuration); // Initialize
                            prerenderRequired = true;
                        }
                        else
                        {
                            Log.Warning($"Can't find soundtrack {_soundtrack.FilePath}");
                            _soundtrack = null;
                        }
                    }
                }

                var rasterizerDesc = new RasterizerStateDescription()
                                         {
                                             FillMode = FillMode.Solid,
                                             CullMode = CullMode.None,
                                             IsScissorEnabled = false,
                                             IsDepthClipEnabled = false
                                         };
                _rasterizerState = new RasterizerState(_device, rasterizerDesc);

                // Sample some frames to preload all shaders and resources
                if (prerenderRequired)
                {
                    _playback.PlaybackSpeed = 0.1f;
                    for (double timeInSecs = 0; timeInSecs < _soundtrack.LengthInSeconds; timeInSecs += 2.0)
                    {
                        Playback.Current.TimeInSecs = timeInSecs;
                        Log.Info($"Pre-evaluate at: {timeInSecs:0.00}s / {Playback.Current.TimeInBars:0.00} bars");

                        DirtyFlag.IncrementGlobalTicks();
                        DirtyFlag.InvalidationRefFrame++;

                        _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, _renderForm.ClientSize.Width, _renderForm.ClientSize.Height, 0.0f, 1.0f));
                        _deviceContext.OutputMerger.SetTargets(_renderView);

                        _evalContext.Reset();
                        _evalContext.RequestedResolution = new Int2(_commandLineOptions.Width, _commandLineOptions.Height);

                        if (_project.Outputs[0] is Slot<Texture2D> textureOutput)
                        {
                            textureOutput.Invalidate();
                            textureOutput.GetValue(_evalContext);

                            var tex = textureOutput.GetValue(_evalContext);
                            if (tex == null)
                            {
                                Log.Error("Failed to initialize texture");
                            }
                        }

                        Thread.Sleep(20);
                        _swapChain.Present(1, PresentFlags.None);
                    }
                }

                // Start playback           
                _playback.Update();
                _playback.TimeInBars = 0;
                _playback.PlaybackSpeed = 1.0;

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Main loop
                RenderLoop.Run(_renderForm, RenderCallback);

                // Release all resources
                _renderView.Dispose();
                _backBuffer.Dispose();
                _deviceContext.ClearState();
                _deviceContext.Flush();
                _device.Dispose();
                _deviceContext.Dispose();
            }
            catch (Exception e)
            {
                Log.Error("Exception in main loop: " + e);
                fileWriter.Dispose(); // flush and close
            }
        }

        private static void RenderCallback()
        {
            WasapiAudioInput.StartFrame(_playback.Settings);
            _playback.Update();

            //Log.Debug($" render at playback time {_playback.TimeInSecs:0.00}s");
            if (_soundtrack != null)
            {
                AudioEngine.UseAudioClip(_soundtrack, _playback.TimeInSecs);
                if (_playback.TimeInSecs >= _soundtrack.LengthInSeconds + _soundtrack.StartTime)
                {
                    if (_commandLineOptions.Loop)
                    {
                        _playback.TimeInSecs = 0.0;
                    }
                    else
                    {
                        Application.Exit();
                    }
                }
            }

            // Update
            AudioEngine.CompleteFrame(_playback, Playback.LastFrameDuration);

            DirtyFlag.IncrementGlobalTicks();
            DirtyFlag.InvalidationRefFrame++;

            _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, _renderForm.ClientSize.Width, _renderForm.ClientSize.Height, 0.0f, 1.0f));
            _deviceContext.OutputMerger.SetTargets(_renderView);

            _evalContext.Reset();
            _evalContext.RequestedResolution = new Int2(_commandLineOptions.Width, _commandLineOptions.Height);

            if (_project.Outputs[0] is Slot<Texture2D> textureOutput)
            {
                textureOutput.Invalidate();
                var outputTexture = textureOutput.GetValue(_evalContext);
                var textureChanged = outputTexture != _outputTexture;
                
                if (_outputTexture != null || textureChanged)
                {
                    _outputTexture = outputTexture;
                    _deviceContext.Rasterizer.State = _rasterizerState;
                    if (_fullScreenVertexShaderResource?.Shader != null)
                        _deviceContext.VertexShader.Set(_fullScreenVertexShaderResource.Shader);
                    if (_fullScreenPixelShaderResource?.Shader != null)
                        _deviceContext.PixelShader.Set(_fullScreenPixelShaderResource.Shader);

                    if (_outputTextureSrv == null || textureChanged)
                    {
                        Log.Debug("Creating new srv...");
                        _outputTextureSrv = new ShaderResourceView(_device, _outputTexture);
                    }
                    
                    _deviceContext.PixelShader.SetShaderResource(0, _outputTextureSrv);

                    _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    _deviceContext.ClearRenderTargetView(_renderView, new Color(0.45f, 0.55f, 0.6f, 1.0f));
                    _deviceContext.Draw(3, 0);
                    _deviceContext.PixelShader.SetShaderResource(0, null);
                }
            }

            _swapChain.Present(_vsync ? 1 : 0, PresentFlags.None);
        }

        private static void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (sender is not Form form)
                return;

            var relativePosition = new Vector2((float)e.X / form.Size.Width,
                                               (float)e.Y / form.Size.Height);

            MouseInput.Set(relativePosition, (e.Button & MouseButtons.Left) != 0);
        }
        

        private static void HandleKeyDown(object sender, KeyEventArgs e)
        {
            var keyIndex = (int)e.KeyCode;
            if (keyIndex >= T3.Core.IO.KeyHandler.PressedKeys.Length)
            {
                Log.Warning($"Ignoring out of range key code {e.KeyCode} with index {keyIndex}");
            }
            else
            {
                T3.Core.IO.KeyHandler.PressedKeys[keyIndex] = true;
            }
        }

        private static void HandleKeyUp(object sender, KeyEventArgs e)
        {
            var keyIndex = (int)e.KeyCode;
            if (keyIndex < T3.Core.IO.KeyHandler.PressedKeys.Length)
            {
                T3.Core.IO.KeyHandler.PressedKeys[keyIndex] = false;
            }
        }

        private static void RebuildBackBuffer(RenderForm form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, ref SwapChain swapChain)
        {
            rtv.Dispose();
            buffer.Dispose();
            swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, SwapChainFlags.AllowModeSwitch);
            buffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            rtv = new RenderTargetView(device, buffer);
        }

        private static Options ParseCommandLine(string[] args)
        {
            Options parsedOptions = null;
            var parser = new Parser(config =>
                                    {
                                        config.HelpWriter = null;
                                        config.AutoVersion = false;
                                    });
            var parserResult = parser.ParseArguments<Options>(args);
            var helpText = HelpText.AutoBuild(parserResult,
                                              h =>
                                              {
                                                  h.AdditionalNewLineAfterOption = false;

                                                  // Todo: This should use information from the main operator
                                                  h.Heading = $"{ProjectSettings.Config.MainOperatorName}";

                                                  // Todo: This should use information from the main operator
                                                  h.Copyright = "Author";
                                                  h.AutoVersion = false;
                                                  return h;
                                              },
                                              e => e);

            parserResult.WithParsed(o => { parsedOptions = o; })
                        .WithNotParsed(o => { Log.Debug(helpText); });
            // use windowed status _only_ when explicitly set, the Options struct doesn't know about this
            if(!args.Any(s => "--windowed".Contains(s))) {
                parsedOptions.Windowed = ProjectSettings.Config.WindowedMode;
            }
            return parsedOptions;
        }

        // Private static bool _inResize;
        private static bool _vsync;
        private static SwapChain _swapChain;
        private static RenderTargetView _renderView;
        private static Texture2D _backBuffer;
        private static SymbolData _symbolData;
        private static Instance _project;
        private static EvaluationContext _evalContext;
        private static Playback _playback;
        private static AudioClip _soundtrack;
        private static DeviceContext _deviceContext;
        private static Options _commandLineOptions;
        private static RenderForm _renderForm;
        private static Texture2D _outputTexture;
        private static ShaderResourceView _outputTextureSrv;
        private static RasterizerState _rasterizerState;
        private static ShaderResource<VertexShader> _fullScreenVertexShaderResource;
        private static ShaderResource<PixelShader> _fullScreenPixelShaderResource;
        private static Device _device;
    }
}