using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.MediaFoundation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator.Interfaces;
using T3.Core.Utils;
using ResourceManager = T3.Core.Resource.ResourceManager;

namespace T3.Operators.Types.Id_914fb032_d7eb_414b_9e09_2bdd7049e049
{
    /** 
     * This code is strongly inspired by
     *
     * https://github.com/vvvv/VL.Video.MediaFoundation/blob/master/src/VideoPlayer.cs
     */
    public class PlayVideo : Instance<PlayVideo>, IStatusProvider
    {
        [Output(Guid = "fa56b47f-1b16-45d5-80cd-32c5a872acf4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        [Output(Guid = "2F16BE73-226B-47E7-B7EE-BF4F3738FA13")]
        public readonly Slot<float> Duration = new();
        
        [Output(Guid = "C89EA3AE-82FF-4791-B755-7B7D9EDDF8A7", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> HasCompleted = new();

        [Output(Guid = "732FC715-A8B5-438F-A607-EE1B8B080C04", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> UpdateCount = new();

        
        public PlayVideo()
        {
            Texture.UpdateAction = Update;
            UpdateCount.UpdateAction = Update;
        }
            
        private void Update(EvaluationContext context)
        {
            // Initialize media foundation library and default values
            if (!_initialized)
            {
                SetupMediaFoundation();
                Volume.TypedDefaultValue.Value = 1.0f;
                ResyncThreshold.TypedDefaultValue.Value = 0.2f;
                _initialized = true;
            }
            
            var url = Path.GetValue(context);
            var pathChanged = url != _url;
            
            if (_engine == null)
            {
                _errorMessageForStatus = "Initialization of MediaEngine failed";
                return;
            }
            
            
            var requestedTime = OverrideTimeInSecs.IsConnected
                                         ? OverrideTimeInSecs.GetValue(context)
                                         : context.Playback.SecondsFromBars(context.LocalTime);
            
            const float completionThreshold =  0.016f; // A hack to prevent engine missing the end of playback

            var durationWithMargin = _engine.Duration - completionThreshold;
            HasCompleted.Value = !_loop && _engine.CurrentTime > durationWithMargin  || requestedTime > durationWithMargin; 
            
            var isSameTime = Math.Abs(context.LocalFxTime - _lastContextTime) < 0.001;
            var dontUpdate = isSameTime && !pathChanged && _hasUpdatedTexture && !_isSeeking;
            if (dontUpdate)
            {
                //Log.Debug($" DontUpdate: {dontUpdate} <-- {_engine.CurrentTime:0.00}/{_engine.Duration}     same:{isSameTime}  pathChanged:{pathChanged} hasTexture:{_hasUpdatedTexture}  isSeeking:{_isSeeking}");
                return;
            }

            _loop = Loop.GetValue(context);
            _lastContextTime = context.LocalFxTime;
            _lastUpdateRunTimeInSecs = Playback.RunTimeInSecs;
            
            if (pathChanged)
            {
                SetMediaUrl(url);
                _engine.Play();
            }


            //Log.Debug($" PlayVideo.Update({shouldBeTimeInSecs:0.00s})", this);
            var clampedSeekTime = _loop? requestedTime % _engine.Duration 
                                      : Math.Clamp(requestedTime, 0.0, _engine.Duration);
            var clampedVideoTime = _loop ? _engine.CurrentTime % _engine.Duration 
                                       : Math.Clamp(_engine.CurrentTime, 0.0, _engine.Duration);
            var deltaTime = clampedSeekTime - clampedVideoTime;
            
            // Play when we are in the center portion of the video
            // and we are playing the video forward
            var isPlayingForward = clampedSeekTime > _lastUpdateTime;
            _play = pathChanged || _loop || Math.Abs(requestedTime - clampedSeekTime) < 0.001f && isPlayingForward;
            _lastUpdateTime = clampedSeekTime;

            // initiate seeking if necessary
            var isCompositionTimelinePlaying = context.Playback.PlaybackSpeed == 0;
            var seekThreshold = isCompositionTimelinePlaying ? 1/120f : ResyncThreshold.GetValue(context);
            var shouldSeek = !_engine.IsSeeking && Math.Abs(deltaTime) > seekThreshold;
            if (shouldSeek)
            {
                //Log.Debug($"Seeking video to {clampedSeekTime:0.000} delta was {deltaTime:0.000)}s", this);
                _seekTime = (float)clampedSeekTime; // + 1.1f/60f;
                _seekRequested = true;
            }

            /***
             * Mute video if audio engine is muted
             * FIXME: does not work when the video is not updating...
             * 
             * Fixing this will require some thought: To managed audio-levels and playback centrally we probably need
             * an interfaces to register all audio sources and provides functions like muting, stop, setting audio level, etc. 
             */
            _engine.Volume = AudioEngine.IsMuted ? 0 : Volume.GetValue(context).Clamp(0f, 1f);
            Duration.Value = _hasUpdatedTexture ? (float)_engine.Duration : -1;
            
            UpdateVideoPlayback();
            if (_hasUpdatedTexture)
            {
                UpdateCount.Value++;
            }
            
            Playback.OpNotReady |= !_hasUpdatedTexture || _isSeeking || _seekRequested;
            Texture.DirtyFlag.Clear();
            Duration.DirtyFlag.Clear();
            HasCompleted.DirtyFlag.Clear();
            UpdateCount.DirtyFlag.Clear();
            
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
                // Add multi thread protection on device (MediaFoundation is multi-threaded)
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

        private void SetupTexture(Int2 size)
        {
            if (size.Width <= 0 || size.Height <= 0 || size.Width > 16383 || size.Height > 16383)
            {
                Log.Warning($"Texture size {size} is invalid, using 512x512 instead.");
                size = new Int2(512, 512);
            }

            Texture.DirtyFlag.Clear();

            if (_texture != null && size == _textureSize)
                return;

            _texture?.Dispose();

            var device = ResourceManager.Device;
            try
            {
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
                _textureSize = size;
            }
            catch (Exception e)
            {
                _errorMessageForStatus = $"Failed to create texture for {size}: {e.Message}";
                _texture = null;
            }
        }

        private void EnginePlaybackEventHandler(MediaEngineEvent mediaEvent, long param1, int param2)
        {
            switch (mediaEvent)
            {
                case MediaEngineEvent.LoadStart:
                    _lastMediaEngineError = MediaEngineErr.Noerror;
                    break;
                case MediaEngineEvent.Error:
                    _lastMediaEngineError = (MediaEngineErr)param1;
                    _errorMessageForStatus = _lastMediaEngineError.ToString();
                    break;

                case MediaEngineEvent.LoadedMetadata:
                    _invalidated = true;
                    _engine.Volume = 0.0;
                    _engine.Pause();
                    break;
                
                case MediaEngineEvent.FirstFrameReady:
                case MediaEngineEvent.TimeUpdate:
                    _lastMediaEngineError = MediaEngineErr.Noerror;

                    // Pause the video to (mute audio) if Update hasn't been
                    // called for a while. This will happen when using PlayVideo
                    // with TimeClips.. 
                    var timeSinceLastUpdate = Playback.RunTimeInSecs - _lastUpdateRunTimeInSecs;
                    if (timeSinceLastUpdate > 2 / 60f)
                    {
                        _engine.Pause();
                    }

                    _errorMessageForStatus = null;

                    // TODO: Pause video if no longer evaluated
                    break;
            }
        }

        private void SetMediaUrl(string url)
        {
            if (url == _url)
                return;

            _url = url;
            try
            {
                _hasUpdatedTexture = false;
                _engine.Pause();
                _engine.Source = url;
            }
            catch (SharpDXException e)
            {
                var unableToSwitchVideoSourceError = "unable to switch video source..." + e.Message;
                _errorMessageForStatus = unableToSwitchVideoSourceError;
                Log.Debug(unableToSwitchVideoSourceError, this);
            }
        }
        

        private void UpdateVideoPlayback()
        {
            if ((ReadyStates)_engine.ReadyState <= ReadyStates.HaveNothing)
            {
                _hasUpdatedTexture = false;
                //_texture = null; // FIXME: this is probably stupid
                return;
            }
            

            if ((ReadyStates)_engine.ReadyState >= ReadyStates.HaveMetadata)
            {
                if (_seekRequested)
                {
                    var seekTime = _seekTime.Clamp(0, (float)_engine.Duration);
                    _engine.CurrentTime = seekTime;
                    _seekOperationStartTime = Playback.RunTimeInSecs;
                    _isSeeking = true;
                    _seekRequested = false;
                }

                _engine.Loop = _loop;

                if (!_engine.Loop)
                {
                    var currentTime = (float)_engine.CurrentTime;
                    const float loopStartTime = 0f;
                    var loopEndTime = (float)_engine.Duration;
                    if (currentTime < loopStartTime || currentTime > loopEndTime)
                    {
                        _engine.CurrentTime = (float)_engine.PlaybackRate >= 0 ? loopStartTime : loopEndTime;
                    }
                }
                //Log.Debug("Play: " + _play);

                if (_play && _engine.IsPaused)
                    _engine.Play();

                else if (!_play && !_engine.IsPaused)
                    _engine.Pause();
            }



            if ((ReadyStates)_engine.ReadyState < ReadyStates.HaveCurrentData || !_engine.OnVideoStreamTick(out var presentationTimeTicks))
            {
                _hasUpdatedTexture = true;
                return;
            }

            if (_isSeeking && !_engine.IsSeeking)
            {
                Log.Debug($"Seeking took {(Playback.RunTimeInSecs - _seekOperationStartTime)*1000:0}ms", this);
                _isSeeking = false;
            }

                
            if (_invalidated || _texture == null)
            {
                _invalidated = false;

                _engine.GetNativeVideoSize(out var width, out var height);
                SetupTexture(new Int2(width, height));

                // _SRGB doesn't work :/ Getting invalid argument exception in TransferVideoFrame
                //_renderTarget = Texture.New2D(graphicsDevice, width, height, PixelFormat.B8G8R8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            }
            
            if (_texture == null)
            {
                _errorMessageForStatus = "Failed to setup texture";
                _hasUpdatedTexture = true;
                return;
            }

            if (presentationTimeTicks == _lastStreamTick)
            {
                _hasUpdatedTexture = false;
                return;
            }

            try
            {

                _engine.TransferVideoFrame(
                                           _texture,
                                           ToVideoRect(default),
                                           //new RawRectangle(0, 0, renderTarget.ViewWidth, renderTarget.ViewHeight),
                                           new RawRectangle(0, 0, _textureSize.Width, _textureSize.Height),
                                           ToRawColorBgra(default));
            }
            catch (Exception e)
            {
                Log.Warning("Using video texture image failed: " + e.Message, SymbolChildId);
                _hasUpdatedTexture = false;
                return;
            }

            _lastStreamTick = presentationTimeTicks;
            Texture.Value = _texture;
            _hasUpdatedTexture = true;

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

            if (_engine == null)
                return;
            
            _engine.Shutdown();
            _engine?.Dispose();
            _texture?.Dispose();
        }


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

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_errorMessageForStatus) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        public string GetStatusMessage()
        {
            return _errorMessageForStatus;
        }


        private long _lastStreamTick;
        private bool _initialized;
        private MediaEngine _engine;
        private DXGIDeviceManager _dxgiDeviceManager;
        private MediaEngineErr _lastMediaEngineError;
        private string _url;
        private Texture2D _texture;
        private Int2 _textureSize = new(0, 0);
        private bool _invalidated;

        /** Set to true to start playback, false to pause playback. */
        private bool _play;
        private float _seekTime;
        private bool _isSeeking;
        private bool _seekRequested;
        private bool _hasUpdatedTexture;
        private bool _loop;

        private double _lastUpdateTime;
        private double _lastContextTime;
        private double _lastUpdateRunTimeInSecs;
        private double _seekOperationStartTime;

        private string _errorMessageForStatus;
        
        
        // Input parameters
        [Input(Guid = "0e255347-08bc-4363-9ffa-ab863a1cea8e")]
        public readonly InputSlot<string> Path = new();

        [Input(Guid = "2FECFBB4-F7D9-4C53-95AE-B64CCBB6FBAD")]
        public readonly InputSlot<float> Volume = new();

        [Input(Guid = "E9C15B3F-8C4A-411D-B9B3-795D64D6BD20")]
        public readonly InputSlot<float> ResyncThreshold = new();

        [Input(Guid = "48E62A3C-A903-4A9B-A44A-148C6C07AC1E")]
        public readonly InputSlot<float> OverrideTimeInSecs = new();

        [Input(Guid = "21B5671B-862F-4CEA-A355-FA019996C936")]
        public readonly InputSlot<bool> Loop = new();


    }
}