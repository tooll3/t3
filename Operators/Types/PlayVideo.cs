using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.MediaFoundation;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using ResourceManager = T3.Core.ResourceManager;

namespace T3.Operators.Types.Id_914fb032_d7eb_414b_9e09_2bdd7049e049
{
    /// <summary>
    /// An attempt to port the original video playback sharpDx example 
    /// </summary>
    /// This code is mostly ported from
    /// https://github.com/vvvv/VL.Video.MediaFoundation/blob/master/src/VideoPlayer.cs
    /// 
    public class PlayVideo : Instance<PlayVideo>
    {
        [Output(Guid = "fa56b47f-1b16-45d5-80cd-32c5a872acf4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        [Output(Guid = "440d022e-f706-4265-9d04-cff099117845")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new Slot<ShaderResourceView>();

        [Input(Guid = "0e255347-08bc-4363-9ffa-ab863a1cea8e")]
        public readonly InputSlot<string> Path = new InputSlot<string>();


        public PlayVideo()
        {
            Texture.UpdateAction = Update;
            ShaderResourceView.UpdateAction = Update;
            
            // renderDrawContextHandle = nodeContext.GetGameProvider()
            //     .Bind(g => RenderContext.GetShared(g.Services).GetThreadContext())
            //     .GetHandle() ?? throw new ServiceNotFoundException(typeof(IResourceProvider<Game>));
            //
            // colorSpaceConverter = new ColorSpaceConverter(renderDrawContextHandle.Resource);

            // Initialize MediaFoundation
            //MediaManagerService.Initialize();

            
        }

        private void Update(EvaluationContext context)
        {
            if (!_initialized)
            {
                SetupMediaFoundation();
                _initialized = true;
            }
            
            if (Texture.DirtyFlag.IsDirty || ShaderResourceView.DirtyFlag.IsDirty)
            {
                SetupTexture(new Size2(512,512));
                //Initialize(filepath: Path.GetValue(context));
            }

            if (_engine == null)
                return;
            
            if(Path.DirtyFlag.IsDirty)
                Url = Path.GetValue(context);

            //TransferFrame();
            UpdateVideo();
        }
        
        
        private bool _initialized; 

        private void SetupMediaFoundation()
        {
            using var mediaEngineAttributes = new MediaEngineAttributes()
                                                  {
                                                      // _SRGB doesn't work :/ Getting invalid argument exception later in TransferVideoFrame
                                                      AudioCategory = SharpDX.Multimedia.AudioStreamCategory.GameMedia,
                                                      AudioEndpointRole = SharpDX.Multimedia.AudioEndpointRole.Multimedia,
                                                      VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                                  };

            //var graphicsDevice = renderDrawContextHandle.Resource.GraphicsDevice;
            var device = ResourceManager.Instance().Device;
            //var device = SharpDXInterop.GetNativeDevice(graphicsDevice) as Device;
            if (device != null)
            {
                // Add multi thread protection on device (MF is multi-threaded)
                using var deviceMultithread = device.QueryInterface<DeviceMultithread>();
                deviceMultithread.SetMultithreadProtected(true);

                // Reset device
                using var manager = new DXGIDeviceManager();
                manager.ResetDevice(device);
                mediaEngineAttributes.DxgiManager = manager;
            }

            // using var classFactory = new MediaEngineClassFactory();
            // try
            // {
            //
            //     _engine = new MediaEngine(classFactory, mediaEngineAttributes);
            //     _engine.PlaybackEvent += Engine_PlaybackEvent;
            // }
            // catch (SharpDXException e)
            // {
            //     Log.Error("Failed to setup MediaEngine: " + e.Message);
            // }
            
            // Setup Media Engine attributes
            // Create a DXGI Device Manager
            dxgiDeviceManager = new DXGIDeviceManager();
            dxgiDeviceManager.ResetDevice(device);            
            var attributes = new MediaEngineAttributes
                                 {
                                     DxgiManager = dxgiDeviceManager,
                                     VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                 };

            MediaManager.Startup();
            using (var factory = new MediaEngineClassFactory())
                _engine = new MediaEngine(factory, attributes, MediaEngineCreateFlags.WaitForStableState, Engine_PlaybackEvent);            
        }
        
        private MediaEngine _engine;
        private DXGIDeviceManager dxgiDeviceManager;
        
        
        private void SetupTexture(Size2 size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                size = new Size2(512, 512);
            
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            _texture = new Texture2D(device,
                                          new Texture2DDescription
                                              {
                                                  ArraySize = 1,
                                                  BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                                                  CpuAccessFlags = CpuAccessFlags.None,
                                                  Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                                                  Width = size.Width,
                                                  Height = size.Height,
                                                  MipLevels = 0,
                                                  OptionFlags = ResourceOptionFlags.None,
                                                  SampleDescription = new SampleDescription(1, 0),
                                                  Usage = ResourceUsage.Default
                                              });
            _textureSrv = new ShaderResourceView(device, _texture);
            //resourceManager.CreateShaderResourceView(_textureResId, "", ref ShaderResourceView.Value);

            Texture.DirtyFlag.Clear();
            ShaderResourceView.DirtyFlag.Clear();
        }

        private Texture2D _texture;
        private ShaderResourceView _textureSrv;
        // private uint _textureResId;
        // private uint _srvResId;
        
        //private readonly IResourceHandle<RenderDrawContext> renderDrawContextHandle;
        //private readonly ColorSpaceConverter colorSpaceConverter;
        private bool _invalidated;
        

        private void Engine_PlaybackEvent(MediaEngineEvent mediaEvent, long param1, int param2)
        {
            Trace.TraceInformation(mediaEvent.ToString());
            switch (mediaEvent)
            {
                case MediaEngineEvent.LoadStart:
                    ErrorCode = MediaEngineErr.Noerror;
                    break;
                case MediaEngineEvent.Error:
                    ErrorCode = (MediaEngineErr)param1;
                    break;
                case MediaEngineEvent.LoadedMetadata:
                    _invalidated = true;
                    break;
            }
        }

        /// <summary>
        /// The URL of the media to play.
        /// </summary>
        public string Url
        {
            set
            {
                if (value != url)
                {
                    url = value;
                    _engine.Source = value;
                }
            }
        }

        string url;

        /// <summary>
        /// Set to true to start playback, false to pause playback.
        /// </summary>
        public bool Play { private get; set; }

        /// <summary>
        /// Gets or sets the rate at which the media is being played back.
        /// </summary>
        public float Rate { get => (float)_engine.PlaybackRate; set => _engine.PlaybackRate = value; }

        public float SeekTime { get; set; }

        public bool Seek { get; set; }

        public float LoopStartTime { get; set; }

        public float LoopEndTime { get; set; } = float.MaxValue;

        public bool Loop { get => _engine.Loop; set => _engine.Loop = value; }

        /// <summary>
        /// The audio volume.
        /// </summary>
        public float Volume { get => (float)_engine.Volume; set => _engine.Volume = value.Clamp(0f, 1f); }

        /// <summary>
        /// The normalized source rectangle.
        /// </summary>
        public RectangleF? SourceBounds { private get; set; }

        /// <summary>
        /// The border color.
        /// </summary>
        public Color4? BorderColor { private get; set; }

        /// <summary>
        /// The size of the output texture. Use zero to take the size from the video.
        /// </summary>
        public Size2 TextureSize
        {
            set
            {
                if (value != _textureSize)
                {
                    _textureSize = value;
                    _invalidated = true;
                }
            }
        }

        private Size2 _textureSize;

        /// <summary>
        /// Whether or not playback started.
        /// </summary>
        public bool Playing => !_engine.IsPaused;

        /// <summary>
        /// A Boolean which is true if the media contained in the element has finished playing.
        /// </summary>
        public bool IsEnded => _engine.IsEnded;

        /// <summary>
        /// The current playback time in seconds
        /// </summary>
        public float CurrentTime => (float)_engine.CurrentTime;

        /// <summary>
        /// The length of the element's media in seconds.
        /// </summary>
        private float Duration => (float)_engine.Duration;

        // /// <summary>
        // /// The current state of the fetching of media over the network.
        // /// </summary>
        // public NetworkState NetworkState => (NetworkState)_engine.NetworkState;

        /// <summary>
        /// The readiness state of the media.
        /// </summary>
        private ReadyState ReadyState => (ReadyState)_engine.ReadyState;

        /// <summary>
        /// Gets the most recent error status.
        /// </summary>
        private MediaEngineErr ErrorCode { get; set; }

        // // This method is not really needed but makes it simpler to work with inside VL
        // public Texture Update(string url,
        //                       bool play = false,
        //                       float rate = 1f,
        //                       float seekTime = 0f,
        //                       bool seek = false,
        //                       float loopStartTime = 0f,
        //                       float loopEndTime = -1f,
        //                       bool loop = false,
        //                       float volume = 1f,
        //                       Size2 textureSize = default,
        //                       RectangleF? sourceBounds = default,
        //                       Color4? borderColor = default)
        // {
        //     Url = url;
        //     Play = play;
        //     Rate = rate;
        //     SeekTime = seekTime;
        //     Seek = seek;
        //     LoopStartTime = loopStartTime;
        //     LoopEndTime = loopEndTime;
        //     Loop = loop;
        //     Volume = volume;
        //     TextureSize = new Size2(textureSize.Width, textureSize.Height);
        //     SourceBounds = sourceBounds;
        //     BorderColor = borderColor;
        //     UpdateVideo();
        //     return _currentVideoFrame;
        // }

        void UpdateVideo()
        {
            if (ReadyState <= ReadyState.HaveNothing)
            {
                _texture = null; // FIXME: this is probably stupid
                return;
            }

            if (ReadyState >= ReadyState.HaveMetadata)
            {
                if (Seek)
                {
                    var seekTime = SeekTime.Clamp(0, Duration);
                    _engine.CurrentTime = seekTime;
                }

                if (Loop)
                {
                    var currentTime = CurrentTime;
                    var loopStartTime = LoopStartTime.Clamp(0f, Duration);
                    var loopEndTime = (LoopEndTime < 0 ? float.MaxValue : LoopEndTime).Clamp(0f, Duration);
                    if (currentTime < loopStartTime || currentTime > loopEndTime)
                    {
                        if (Rate >= 0)
                            _engine.CurrentTime = loopStartTime;
                        else
                            _engine.CurrentTime = loopEndTime;
                    }
                }

                if (Play && _engine.IsPaused)
                    _engine.Play();
                else if (!Play && !_engine.IsPaused)
                    _engine.Pause();
            }

            if (ReadyState >= ReadyState.HaveCurrentData && _engine.OnVideoStreamTick(out var presentationTimeTicks))
            {
                if (_invalidated || _texture == null)// || _currentVideoFrame is null)
                {
                    
                    _invalidated = false;

                    //_renderTarget?.Dispose();
                    //_texture?.Dispose();

                    _engine.GetNativeVideoSize(out var width, out var height);

                    var size = new Size2(width, height);
                    Log.Debug($"should set size to: {width}x{height}");
                    SetupTexture(size);
                    // Apply user specified size
                    //var x = _textureSize;
                    // if (x.Width > 0)
                    //     width = x.Width;
                    // if (x.Height > 0)
                    //     height = x.Height;

                    var graphicsDevice = ResourceManager.Instance().Device; // renderDrawContextHandle.Resource.GraphicsDevice;

                    // _SRGB doesn't work :/ Getting invalid argument exception in TransferVideoFrame
                    //_renderTarget = Texture.New2D(graphicsDevice, width, height, PixelFormat.B8G8R8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
                }

                //if (SharpDXInterop.GetNativeResource(renderTarget) is Texture2D nativeRenderTarget)
                //{
                if (_texture != null)
                    _engine.TransferVideoFrame(
                                              _texture,
                                              ToVideoRect(SourceBounds),
                                              //new RawRectangle(0, 0, renderTarget.ViewWidth, renderTarget.ViewHeight),
                                              new RawRectangle(0, 0, _textureSize.Width, _textureSize.Height),
                                              ToRawColorBGRA(BorderColor));

                //}
                // Apply color space conversion if necessary
                //currentVideoFrame = colorSpaceConverter.ToDeviceColorSpace(renderTarget);
                //_currentVideoFrame = _texture;
            }
        }

        //private int _width;
        //private int _height;

        //private Texture _renderTarget;
        //private Texture _currentVideoFrame;

        static VideoNormalizedRect? ToVideoRect(RectangleF? rect)
        {
            if (rect.HasValue)
            {
                var r = rect.Value;
                return new VideoNormalizedRect()
                           {
                               Left = r.Left.Clamp(0f, 1f),
                               Bottom = r.Bottom.Clamp(0f, 1f),
                               Right = r.Right.Clamp(0f, 1f),
                               Top = r.Top.Clamp(0f, 1f)
                           };
            }

            return default;
        }

        static RawColorBGRA? ToRawColorBGRA(Color4? color)
        {
            if (color.HasValue)
            {
                color.Value.ToBgra(out var r, out var g, out var b, out var a);
                return new RawColorBGRA(b, g, r, a);
            }

            return default;
        }

        // FIXME: we should call this properly
        // public void Dispose()
        // {
        //     _engine.Shutdown();
        //     _engine.PlaybackEvent -= Engine_PlaybackEvent;
        //     _engine.Dispose();
        //     //colorSpaceConverter.Dispose();
        //     renderTarget?.Dispose();
        // }
    }

    // public enum NetworkState : short
    // {
    //     /// <summary>
    //     /// There is no data yet. Also, readyState is HaveNothing.
    //     /// </summary>
    //     Empty,
    //
    //     /// <summary>
    //     /// HTMLMediaElement is active and has selected a resource, but is not using the network.
    //     /// </summary>
    //     Idle,
    //
    //     /// <summary>
    //     /// The browser is downloading HTMLMediaElement data.
    //     /// </summary>
    //     Loading,
    //
    //     /// <summary>
    //     /// No HTMLMediaElement src found.
    //     /// </summary>
    //     NoSource
    // }

    public enum ReadyState : short
    {
        /// <summary>
        /// No information is available about the media resource.
        /// </summary>
        HaveNothing,

        /// <summary>
        /// Enough of the media resource has been retrieved that the metadata attributes are initialized. Seeking will no longer raise an exception.
        /// </summary>
        HaveMetadata,

        /// <summary>
        /// Data is available for the current playback position, but not enough to actually play more than one frame.
        /// </summary>
        HaveCurrentData,

        /// <summary>
        /// Data for the current playback position as well as for at least a little bit of time into the future is available (in other words, at least two frames of video, for example).
        /// </summary>
        HaveFutureData,

        /// <summary>
        /// Enough data is available—and the download rate is high enough—that the media can be played through to the end without interruption.
        /// </summary>
        HaveEnoughData
    }
}

