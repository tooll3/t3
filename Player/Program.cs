#define FORCE_D3D_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using ManagedBass;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Compilation;
using T3.Core.DataTypes.Vector;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.App;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;
using Vector2 = System.Numerics.Vector2;
using SharpDX.Windows;
using T3.SystemUi;
using ResourceManager = T3.Core.Resource.ResourceManager;

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
            var logDirectory = Path.Combine(Core.UserData.UserData.SettingsFolder, "log");
            var fileWriter = FileWriter.CreateDefault(logDirectory);
            CoreUi.Instance = new MsForms.MsForms();
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
                
                var resolution = _commandLineOptions.Size;             
                
                var iconPath = Path.Combine(RuntimeAssemblies.CoreDirectory, "Resources", "t3-editor", "images", "t3.ico");
                var gotIcon = File.Exists(iconPath);
                
                Icon icon;
                if (!gotIcon)
                {
                    Log.Warning("Failed to load icon");
                    icon = null;
                }
                else
                {
                    icon = new Icon(iconPath);
                }

                _renderForm = new RenderForm(ProjectSettings.Config.MainOperatorName)
                                  {
                                      ClientSize = resolution,
                                      AllowUserResizing = false,
                                      Icon = icon,
                                  };
                
                var handle = _renderForm.Handle;

                // SwapChain description
                var desc = new SwapChainDescription
                               {
                                   BufferCount = 3,
                                   ModeDescription = new ModeDescription(resolution.Width, resolution.Height,
                                                                         new Rational(60, 1), Format.R8G8B8A8_UNorm),
                                   IsWindowed = _commandLineOptions.Windowed,
                                   OutputHandle = handle,
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
                ResourceManager.Instance().Init(_device);
                SharedResources.Initialize();

                var cursor = CoreUi.Instance.Cursor;

                if (_swapChain.IsFullScreen)
                {
                    cursor.SetVisible(false);
                }

                // Ignore all windows events
                var factory = _swapChain.GetParent<Factory>();
                factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);

                var startedWindowed = _commandLineOptions.Windowed;

                MsForms.MsForms.TrackKeysOf(_renderForm);

                _renderForm.KeyUp += (sender, keyArgs) =>
                                     {
                                         if (startedWindowed && keyArgs.Alt && keyArgs.KeyCode == Keys.Enter)
                                         {
                                             _swapChain.IsFullScreen = !_swapChain.IsFullScreen;
                                             RebuildBackBuffer(_renderForm, _device, ref _renderView, ref _backBuffer, ref _swapChain);
                                             cursor.SetVisible(!_swapChain.IsFullScreen);
                                         }

                                         var currentPlayback = Playback.Current;
                                         if (ProjectSettings.Config.EnablePlaybackControlWithKeyboard)
                                         {
                                             switch (keyArgs.KeyCode)
                                             {
                                                 case Keys.Left:
                                                     currentPlayback.TimeInBars -= 4;
                                                     break;
                                                 case Keys.Right:
                                                     currentPlayback.TimeInBars += 4;
                                                     break;
                                                 case Keys.Space:
                                                     currentPlayback.PlaybackSpeed = Math.Abs(currentPlayback.PlaybackSpeed) > 0.01f ? 0 : 1;
                                                     break;
                                             }
                                         }

                                         if (keyArgs.KeyCode == Keys.Escape)
                                         {
                                             CoreUi.Instance.ExitApplication();
                                         }
                                     };

                _renderForm.MouseMove += MouseMoveHandler;
                _renderForm.MouseClick += MouseMoveHandler;

                // New RenderTargetView from the backbuffer
                _backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);
                _renderView = new RenderTargetView(_device, _backBuffer);

                var shaderCompiler = new DX11ShaderCompiler
                                         {
                                             Device = _device
                                         };
                ShaderCompiler.Instance = shaderCompiler;
                _fullScreenPixelShaderResource = SharedResources.FullScreenPixelShaderResource;
                _fullScreenVertexShaderResource = SharedResources.FullScreenVertexShaderResource;

                LoadOperators();

                var demoSymbol = SymbolRegistry.Entries[ProjectSettings.Config.MainOperatorGuid];

                var playbackSettings = demoSymbol.PlaybackSettings;
                _playback = new Playback
                                {
                                    Settings = playbackSettings
                                };

                // Create instance of project op, all children are create automatically
                _project = demoSymbol.CreateInstance(Guid.NewGuid(), null);
                _evalContext = new EvaluationContext();

                var prerenderRequired = false;

                Bass.Free();
                Bass.Init();

                // Init wasapi input if required
                if (playbackSettings is { AudioSource: PlaybackSettings.AudioSources.ProjectSoundTrack } && playbackSettings.GetMainSoundtrack(out _soundtrack))
                {
                    if (_soundtrack.TryGetAbsoluteFilePath(out var _))
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

        private static void LoadOperators()
        {
            var assemblies = Directory.GetFiles(RuntimeAssemblies.CoreDirectory, "*.dll")
                                      .Where(path => Path.GetFileName(path) != "System.Windows.Forms.dll")
                                      .Select(file =>
                                              {
                                                  RuntimeAssemblies.TryLoadAssemblyInformation(file, out var info);
                                                  return info;
                                              })
                                      .Where(info => info != null)
                                      .Where(info => info.IsOperatorAssembly);

            var packageLoadInfo = assemblies
                                 .AsParallel()
                                 .Select(assemblyInfo =>
                                         {
                                             var symbolPackage = new PlayerSymbolPackage(assemblyInfo);
                                             symbolPackage.LoadSymbols(false, out var newSymbolsWithFiles, out var allNewSymbols);
                                             return new PackageLoadInfo(symbolPackage, newSymbolsWithFiles, allNewSymbols);
                                         })
                                 .ToArray();

            packageLoadInfo
               .AsParallel()
               .ForAll(packageInfo => packageInfo.Package.ApplySymbolChildren(packageInfo.NewlyLoadedSymbols));
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
                        CoreUi.Instance.ExitApplication();
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

        private static void MouseMoveHandler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (sender is not Form form)
                return;

            var relativePosition = new Vector2((float)e.X / form.Size.Width,
                                               (float)e.Y / form.Size.Height);

            MouseInput.Set(relativePosition, (e.Button & MouseButtons.Left) != 0);
        }

        private static void RebuildBackBuffer(RenderForm form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, ref SwapChain swapChain)
        {
            rtv.Dispose();
            buffer.Dispose();
            swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, SwapChainFlags.AllowModeSwitch);
            buffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
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
            if (!args.Any(s => "--windowed".Contains(s)))
            {
                parsedOptions.Windowed = ProjectSettings.Config.WindowedMode;
            }

            return parsedOptions;
        }

        private readonly struct PackageLoadInfo(
            PlayerSymbolPackage package,
            List<SymbolJson.SymbolReadResult> newlyLoadedSymbols,
            IReadOnlyCollection<Symbol> allNewSymbols)
        {
            public readonly PlayerSymbolPackage Package = package;
            public readonly List<SymbolJson.SymbolReadResult> NewlyLoadedSymbols = newlyLoadedSymbols;
            public readonly IReadOnlyCollection<Symbol> AllNewSymbols = allNewSymbols;
        }

        // Private static bool _inResize;
        private static bool _vsync;
        private static SwapChain _swapChain;
        private static RenderTargetView _renderView;
        private static Texture2D _backBuffer;
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