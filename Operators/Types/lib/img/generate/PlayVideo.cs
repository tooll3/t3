using System;
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
using Core.Audio;
using T3.Core.Animation;
using ResourceManager = T3.Core.ResourceManager;

namespace T3.Operators.Types.Id_914fb032_d7eb_414b_9e09_2bdd7049e049
{
    /** 
     * This code is strongly inspired by
     *
     * https://github.com/vvvv/VL.Video.MediaFoundation/blob/master/src/VideoPlayer.cs
     */
    public class PlayVideo : Instance<PlayVideo>
    {
        [Output(Guid = "fa56b47f-1b16-45d5-80cd-32c5a872acf4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        // Input parameters
        [Input(Guid = "0e255347-08bc-4363-9ffa-ab863a1cea8e")]
        public readonly InputSlot<string> Path = new();

        [Input(Guid = "2FECFBB4-F7D9-4C53-95AE-B64CCBB6FBAD")]
        public readonly InputSlot<float> Volume = new();

        [Input(Guid = "E9C15B3F-8C4A-411D-B9B3-795D64D6BD20")]
        public readonly InputSlot<float> ResyncThreshold = new();

        public PlayVideo()
        {
            Texture.UpdateAction = Update;
        }

        
        private void Update(EvaluationContext context)
        {
            _lastUpdateRunTimeInSecs = Playback.RunTimeInSecs;
            
            // Initialize media foundation library and default values
            if (!_initialized)
            {
                SetupMediaFoundation();
                Volume.TypedDefaultValue.Value = 1.0f;
                ResyncThreshold.TypedDefaultValue.Value = 0.2f;
                _initialized = true;
            }

            // Change texture size if necessary
            if (Texture.DirtyFlag.IsDirty
                || _size.Width <= 0 || _size.Height <= 0)
            {
                SetupTexture(_size);
            }

            if (_engine == null)
                return;

            // Update video if url has changed
            if (Path.DirtyFlag.IsDirty)
            {
                _play = true;
                MediaUrl = Path.GetValue(context);
                _engine.Play();
            }

            // shall we seek?
            var shouldBeTimeInSecs = context.Playback.SecondsFromBars(context.LocalTime);
            //Log.Debug($" PlayVideo.Update({shouldBeTimeInSecs:0.00s})");
            var clampedSeekTime = Math.Clamp(shouldBeTimeInSecs, 0.0, _engine.Duration);
            var clampedVideoTime = Math.Clamp(_engine.CurrentTime, 0.0, _engine.Duration);
            var deltaTime = clampedSeekTime - clampedVideoTime;
            var shouldSeek = !_engine.IsSeeking
                             && Math.Abs(deltaTime) > ResyncThreshold.GetValue(context);

            // Play when we are in the center portion of the video
            // and we are playing the video forward
            var isPlayingForward = (clampedSeekTime - _lastUpdateTime > 0.0);
            _play = (shouldBeTimeInSecs == clampedSeekTime) && isPlayingForward;
            _lastUpdateTime = clampedSeekTime;

            // initiate seeking if necessary
            if (shouldSeek)
            {
                Log.Debug($"Seeked video to {clampedSeekTime:0.00} delta was {deltaTime:0.0000)}s");
                SeekTime = (float)clampedSeekTime + 1.1f/60f;
                Seek = true;
            }

            /***
             * Mute video if audio engine is muted
             * FIXME: does not work when the video is not updating...
             * 
             * Fixing this will require some thought: To managed audio-levels and playback centrally we probably need
             * an interfaces to register all audio sources and provides functions like muting, stop, setting audio level, etc. 
             */
            if (AudioEngine.IsMuted)
            {
                _engine.Volume = 0.0;
            }
            else
            {
                _engine.Volume = Volume.GetValue(context).Clamp(0f, 1f);
            }

            UpdateVideo();
        }

        private void SetupMediaFoundation()
        {
            using var mediaEngineAttributes = new MediaEngineAttributes
                                                  {
                                                      // _SRGB doesn't work :/ Getting invalid argument exception later in TransferVideoFrame
                                                      AudioCategory = SharpDX.Multimedia.AudioStreamCategory.GameMedia,
                                                      AudioEndpointRole = SharpDX.Multimedia.AudioEndpointRole.Multimedia,
                                                      VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                                  };

            var device = ResourceManager.Device;
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

            // Setup Media Engine attributes and create a DXGI Device Manager
            _dxgiDeviceManager = new DXGIDeviceManager();
            _dxgiDeviceManager.ResetDevice(device);
            var attributes = new MediaEngineAttributes
                                 {
                                     DxgiManager = _dxgiDeviceManager,
                                     VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                     //VideoOutputFormat = (int)SharpDX.DXGI.Format.NV12                                     
                                 };

            MediaManager.Startup();
            using var factory = new MediaEngineClassFactory();
            _engine = new MediaEngine(factory, attributes, MediaEngineCreateFlags.None, EnginePlaybackEventHandler);
        }

        private void SetupTexture(Size2 size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                size = new Size2(512, 512);

            Texture.DirtyFlag.Clear();

            if (_texture != null && size == _size)
                return;

            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
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
            _size = size;
        }

        private void EnginePlaybackEventHandler(MediaEngineEvent mediaEvent, long param1, int param2)
        {
            switch (mediaEvent)
            {
                case MediaEngineEvent.LoadStart:
                    LastErrorCode = MediaEngineErr.Noerror;
                    break;
                case MediaEngineEvent.Error:
                    LastErrorCode = (MediaEngineErr)param1;
                    break;
                case MediaEngineEvent.LoadedMetadata:
                    _invalidated = true;
                    _engine.Volume = 0.0;
                    Log.Debug("pausing...");
                    _engine.Pause();
                    break;
                case MediaEngineEvent.FirstFrameReady:
                case MediaEngineEvent.TimeUpdate:
                    LastErrorCode = MediaEngineErr.Noerror;

                    // Pause the video to (mute audio) if Update hasn't been
                    // called for a while. This will happen when using PlayVideo
                    // with TimeClips.. 
                    var timeSinceLastUpdate = Playback.RunTimeInSecs - _lastUpdateRunTimeInSecs;
                    if (timeSinceLastUpdate > 2 / 60f)
                    {
                        _engine.Pause();
                    }
                    
                    // TODO: Pause video if no longer evaluated
                    break;
            }
        }
        
        private string MediaUrl
        {
            set
            {
                if (value != _url)
                {
                    _url = value;
                    try
                    {
                        _engine.Pause();
                        _engine.Source = value;
                    }
                    catch (SharpDXException e)
                    {
                        Log.Debug("unable to switch video source..." + e.Message);
                    }
                }
            }
        }

        private MediaEngine _engine;
        private DXGIDeviceManager _dxgiDeviceManager;
        private Size2 _size = new Size2(0, 0);

        private string _url;
        private Texture2D _texture;
        private bool _invalidated;

        /** Set to true to start playback, false to pause playback. */
        private bool _play;

        private float SeekTime { get; set; }
        private bool Seek { get; set; }
        private float LoopStartTime { get; set; }
        private float LoopEndTime { get; set; } = float.MaxValue;

        /** The normalized source rectangle. */
        private RectangleF? SourceBounds { get; set; }

        /** The border color. */
        private Color4? BorderColor { get; set; }

        /** The size of the output texture. Use zero to take the size from the video. */
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

        /** Gets the most recent error status. */
        private MediaEngineErr LastErrorCode { get; set; }

        private void UpdateVideo()
        {
            if (ReadyState <= ReadyStates.HaveNothing)
            {
                _texture = null; // FIXME: this is probably stupid
                return;
            }

            if (ReadyState >= ReadyStates.HaveMetadata)
            {
                if (Seek)
                {
                    var seekTime = SeekTime.Clamp(0, Duration);
                    _engine.CurrentTime = seekTime;
                    Seek = false;
                }

                if (Loop)
                {
                    var currentTime = CurrentTime;
                    var loopStartTime = LoopStartTime.Clamp(0f, Duration);
                    var loopEndTime = (LoopEndTime < 0 ? float.MaxValue : LoopEndTime).Clamp(0f, Duration);
                    if (currentTime < loopStartTime || currentTime > loopEndTime)
                    {
                        if (PlaybackRate >= 0)
                            _engine.CurrentTime = loopStartTime;
                        else
                            _engine.CurrentTime = loopEndTime;
                    }
                }

                if (_play && _engine.IsPaused)
                    _engine.Play();

                else if (!_play && !_engine.IsPaused)
                    _engine.Pause();
            }

            if (ReadyState < ReadyStates.HaveCurrentData || !_engine.OnVideoStreamTick(out var presentationTimeTicks))
                return;

            if (_invalidated || _texture == null)
            {
                _invalidated = false;

                _engine.GetNativeVideoSize(out var width, out var height);
                Log.Debug($"should set size to: {width}x{height}");
                SetupTexture(new Size2(width, height));

                // _SRGB doesn't work :/ Getting invalid argument exception in TransferVideoFrame
                //_renderTarget = Texture.New2D(graphicsDevice, width, height, PixelFormat.B8G8R8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            }

            if (_texture == null)
                return;

            _engine.TransferVideoFrame(
                                       _texture,
                                       ToVideoRect(SourceBounds),
                                       //new RawRectangle(0, 0, renderTarget.ViewWidth, renderTarget.ViewHeight),
                                       new RawRectangle(0, 0, _textureSize.Width, _textureSize.Height),
                                       ToRawColorBgra(BorderColor));
            Texture.Value = _texture;
        }

        private static VideoNormalizedRect? ToVideoRect(RectangleF? rect)
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

        private static RawColorBGRA? ToRawColorBgra(Color4? color)
        {
            if (color.HasValue)
            {
                color.Value.ToBgra(out var r, out var g, out var b, out var a);
                return new RawColorBGRA(b, g, r, a);
            }

            return default;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;
            
            Log.Debug(" Disposing video");

            //base.Dispose();
            if (_engine != null)
            {
                _engine.Shutdown();
                //_engine.PlaybackEvent -= EnginePlaybackEventHandler;
                _engine.Dispose();
                _texture.Dispose();
            }
            //colorSpaceConverter.Dispose();
            //renderTarget?.Dispose();
        }

        #region Forward engine properties
        
        private float PlaybackRate { get => (float)_engine.PlaybackRate; set => _engine.PlaybackRate = value; }
        
        private bool Loop { get => _engine.Loop; set => _engine.Loop = value; }
        
        /** Whether or not playback started. */
        public bool Playing => !_engine.IsPaused;

        /** A Boolean which is true if the media contained in the element has finished playing. */
        public bool IsEnded => _engine.IsEnded;

        /** The current playback time in seconds */
        private float CurrentTime => (float)_engine.CurrentTime;

        /** The length of the element's media in seconds. */
        private float Duration => (float)_engine.Duration;

        /** The readiness state of the media. */
        private ReadyStates ReadyState => (ReadyStates)_engine.ReadyState;
        #endregion

        private enum ReadyStates : short
        {
            /** information is available about the media resource. */
            HaveNothing,

            /** ugh of the media resource has been retrieved that the metadata attributes are initialized. Seeking will no longer raise an exception. */
            HaveMetadata,

            /** a is available for the current playback position, but not enough to actually play more than one frame. */
            HaveCurrentData,

            /** a for the current playback position as well as for at least a little bit of time into the future is available (in other words, at least two frames of video, for example). */
            HaveFutureData,

            /** ugh data is available—and the download rate is high enough—that the media can be played through to the end without interruption.*/
            HaveEnoughData
        }

        private bool _initialized;
        private double _lastUpdateTime;
        private double _lastUpdateRunTimeInSecs = 0;

    }
}