using NAudio.Wave;
using NewTek;
using NewTek.NDI;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Resource = SharpDX.Direct3D11.Resource;
using PixelFormat = SharpDX.WIC.PixelFormat;
using System.Diagnostics;

namespace T3.Operators.Types.Id_7567c3b0_9d91_40d2_899d_3a95b481d023
{
    public class NdiInput : Instance<NdiInput>
    {
        [Output(Guid = "85F1AF38-074E-475D-94F5-F48079979509", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        [Output(Guid = "85F1AF38-074E-475D-94F5-F48079979545", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> UploadTime = new();

        public NdiInput()
        {
            InitializeNdi();
            _findInstance = new Finder(true);
            Texture.UpdateAction = Update;
            UploadTime.UpdateAction = Update;
            SourceNumber.DirtyFlag.Clear();
        }
        ~NdiInput()
        {
            Dispose(false);
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);

            if (SourceNumber.DirtyFlag.IsDirty)
            {
                var sourceIndex = SourceNumber.GetValue(context);
                if (sourceIndex >= 0 && sourceIndex < _findInstance.Sources.Count)
                {
                    SourceName.SetTypedInputValue(_findInstance.Sources[sourceIndex].Name);
                }
                SourceNumber.DirtyFlag.Clear();
            }

            if (SourceName.DirtyFlag.IsDirty)
            {
                _sourceName = SourceName.GetValue(context);

                var isNumeric = int.TryParse(_sourceName, out int sourceIndex);
                if (isNumeric && sourceIndex < _findInstance.Sources.Count)
                {
                    _sourceName = _findInstance.Sources[sourceIndex].Name;
                }

                if (_sourceName.Contains(" ("))
                {
                    _textureMutex.WaitOne(Timeout.Infinite);
                    DisposeTextures();
                    Connect(new Source(_sourceName));
                    _textureMutex.ReleaseMutex();
                }
            }

            _textureMutex.WaitOne(Timeout.Infinite);
            _textureAction?.Invoke();
            _textureAction = null;
            _textureMutex.ReleaseMutex();
        }

        private bool InitializeNdi()
        {
            // Not required, but "correct". (see the SDK documentation)
            if (!_initialized)
            {
                _initialized = NDIlib.initialize();
                if (!_initialized)
                {
                    // Cannot run NDI. Most likely because the CPU is not sufficient (see SDK documentation).
                    // you can check this directly with a call to NDIlib.is_supported_CPU()
                    if (!NDIlib.is_supported_CPU())
                    {
                        Log.Error("CPU unsupported.", this);
                    }
                    else
                    {
                        // not sure why, but it's not going to run
                        Log.Error("Cannot run NDI.", this);
                    }
                }
            }

            return _initialized;
        }

        // This will find NDI sources on the network.
        // Continually updated as new sources arrive.
        // Note that this example does see local sources (new Finder(true))
        // This is for ease of testing, but normally is not needed in released products.
        public Finder FindInstance
        {
            get { return _findInstance; }
        }

        // connect to an NDI source in our Dictionary by name
        private void Connect(Source source)
        {
            // Increment the receiver Id, meaning we have a new source to work with. If
            // there's already another receiver thread running, the commands it has
            // sent to the UI won't be processed.
            int receiverId = Interlocked.Increment(ref _receiverId);

            // before we are connected, we need to set up our image
            // it's bad practice to do this in the constructor
            //if (Child == null)
            //    Child = VideoSurface;

            // just to be safe
            Disconnect();

            // Sanity
            if (source == null || String.IsNullOrEmpty(source.Name))
                return;

            if (String.IsNullOrEmpty(_sourceName))
                throw new ArgumentException("Receiver name can not be null or empty.", _sourceName);

            // a source_t to describe the source to connect to.
            NDIlib.source_t source_t = new NDIlib.source_t()
            {
                p_ndi_name = UTF.StringToUtf8(source.Name)
            };

            // make a description of the receiver we want
            NDIlib.recv_create_v3_t recvDescription = new NDIlib.recv_create_v3_t()
            {
                // the source we selected
                source_to_connect_to = source_t,

                // we want BGRA frames for this example
                color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,

                // we want full quality - for small previews or limited bandwidth, choose lowest
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,

                // let NDIlib deinterlace for us if needed
                allow_video_fields = false,

                // The name of the NDI receiver to create. This is a NULL terminated UTF8 string and should be
                // the name of receive channel that you have. This is in many ways symettric with the name of
                // senders, so this might be "Channel 1" on your system.
                p_ndi_recv_name = UTF.StringToUtf8(_sourceName)
            };

            // create a new instance connected to this source
            _recvInstancePtr = NDIlib.recv_create_v3(ref recvDescription);

            // free the memory we allocated with StringToUtf8
            Marshal.FreeHGlobal(source_t.p_ndi_name);
            Marshal.FreeHGlobal(recvDescription.p_ndi_recv_name);

            // did it work?
            System.Diagnostics.Debug.Assert(_recvInstancePtr != IntPtr.Zero, "Failed to create NDI receive instance.");

            if (_recvInstancePtr != IntPtr.Zero)
            {
                // We are now going to mark this source as being on program output for tally purposes (but not on preview)
                SetTallyIndicators(true, false);

                // start up a thread to receive on
                _receiveThread = new Thread(ReceiveThreadProc) { IsBackground = true, Name = "NdiExampleReceiveThread" };

                // Pass the current receiver Id to the new thread
                _receiveThread.Start(receiverId);
            }
        }

        public void Disconnect()
        {
            // in case we're connected, reset the tally indicators
            SetTallyIndicators(false, false);

            // check for a running thread
            if (_receiveThread != null)
            {
                // tell it to exit
                _exitThread = true;

                // wait for it to end
                _receiveThread.Join();
            }

            // reset thread defaults
            _receiveThread = null;
            _exitThread = false;

            // Destroy the receiver
            NDIlib.recv_destroy(_recvInstancePtr);

            // set it to a safe value
            _recvInstancePtr = IntPtr.Zero;

            // set function status to defaults
            //IsPtz = false;
            //IsRecordingSupported = false;
            //WebControlUrl = String.Empty;
        }

        void SetTallyIndicators(bool onProgram, bool onPreview)
        {
            // we need to have a receive instance
            if (_recvInstancePtr != IntPtr.Zero)
            {
                // set up a state descriptor
                NDIlib.tally_t tallyState = new NDIlib.tally_t()
                {
                    on_program = onProgram,
                    on_preview = onPreview
                };

                // set it on the receiver instance
                NDIlib.recv_set_tally(_recvInstancePtr, ref tallyState);
            }
        }

        // the receive thread runs though this loop until told to exit
        void ReceiveThreadProc(object param)
        {
            var device = ResourceManager.Device;

            // Here we keep track of the receiver Id used for this thread.
            int currReceiverId = (int)param;

            while (!_exitThread && _recvInstancePtr != IntPtr.Zero)
            {
                // The descriptors
                NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t();
                NDIlib.audio_frame_v2_t audioFrame = new NDIlib.audio_frame_v2_t();
                NDIlib.metadata_frame_t metadataFrame = new NDIlib.metadata_frame_t();

                switch (NDIlib.recv_capture_v2(_recvInstancePtr, ref videoFrame, ref audioFrame, ref metadataFrame, 1000))
                {
                    // No data
                    case NDIlib.frame_type_e.frame_type_none:
                        // No data received
                        break;

                    // frame settings - check for extended functionality
                    case NDIlib.frame_type_e.frame_type_status_change:
                        // check for PTZ
                        //IsPtz = NDIlib.recv_ptz_is_supported(_recvInstancePtr);

                        // Check for recording
                        //IsRecordingSupported = NDIlib.recv_recording_is_supported(_recvInstancePtr);

                        // Check for a web control URL
                        // We must free this string ptr if we get one.
                        /*
                        IntPtr webUrlPtr = NDIlib.recv_get_web_control(_recvInstancePtr);
                        if (webUrlPtr == IntPtr.Zero)
                        {
                            WebControlUrl = String.Empty;
                        }
                        else
                        {
                            // convert to managed String
                            WebControlUrl = NDI.UTF.Utf8ToString(webUrlPtr);

                            // Don't forget to free the string ptr
                            NDIlib.recv_free_string(_recvInstancePtr, webUrlPtr);
                        }
                        */

                        break;

                    // Video data
                    case NDIlib.frame_type_e.frame_type_video:

                        // if not enabled, just discard
                        // this can also occasionally happen when changing sources
                        if (!_videoEnabled || videoFrame.p_data == IntPtr.Zero)
                        {
                            // always free received frame
                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                            break;
                        }

                        // get all our info so that we can free the frame
                        int yres = (int)videoFrame.yres;
                        int xres = (int)videoFrame.xres;
                        int stride = (int)videoFrame.line_stride_in_bytes;

                        // Try to acquire our texture mutex for some time.
                        // End processing if we shall quit
                        while (!_textureMutex.WaitOne(10))
                        {
                            if (_exitThread)
                            {
                                // always free received frame
                                NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                                break;
                            }
                        }

                        // For now, we need to be on the UI thread to fill our bitmap
                        _textureAction = new Action(() =>
                        {
                            // If the local receiver Id is not the same as the global receiver Id,
                            // then that means that either the connection source has changed, or
                            // the window has closed, in which case the latest receiver Id
                            // will be 0. If either is true, we stop processing data.
                            if (currReceiverId != _receiverId || _receiverId == 0)
                            {
                                return;
                            }

                            if (xres == 0 || yres == 0 || stride == 0)
                            {
                                // always free received frames
                                NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                                return;
                            }

                            // create several textures with a given format with CPU access
                            // to be able to read out the initial texture values
                            SharpDX.DXGI.Format textureFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
                            if (ImagesWithCpuAccess.Count == 0
                                || ImagesWithCpuAccess[0].Description.Format != textureFormat
                                || ImagesWithCpuAccess[0].Description.Width != (int)xres
                                || ImagesWithCpuAccess[0].Description.Height != (int)yres
                                || ImagesWithCpuAccess[0].Description.MipLevels != 1)
                            {
                                var imageDesc = new Texture2DDescription
                                {
                                    BindFlags = BindFlags.ShaderResource,
                                    Format = textureFormat,
                                    Width = (int)xres,
                                    Height = (int)yres,
                                    MipLevels = 1,
                                    SampleDescription = new SampleDescription(1, 0),
                                    Usage = ResourceUsage.Dynamic,
                                    OptionFlags = ResourceOptionFlags.None,
                                    CpuAccessFlags = CpuAccessFlags.Write,
                                    ArraySize = 1
                                };

                                DisposeTextures();

                                Log.Debug($"NDI input wxh = {xres}x{yres}, " +
                                            $"format = {textureFormat} ({textureFormat})");

                                for (var i = 0; i < NumTextureEntries; ++i)
                                {
                                    ImagesWithCpuAccess.Add(new Texture2D(device, imageDesc));
                                }

                                _currentIndex = 0;
                            }

                            // copy the spout texture to an internal image
                            var immediateContext = device.ImmediateContext;
                            var writableImage = ImagesWithCpuAccess[_currentIndex];
                            _currentIndex = (_currentIndex + 1) % NumTextureEntries;

                            // we have to map with a stride that represents multiples of 16 pixels here
                            // (it is yet unclear why, but works)
                            var formatId = PixelFormat.Format32bppBGRA;
                            int outputStride = PixelFormat.GetStride(formatId, ((xres+15)/16)*16);

                            // map resource manually using our stride...
                            int mipSize;
                            DataBox dataBox = immediateContext.MapSubresource((Resource)writableImage, 0, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out mipSize);

                            Stopwatch sw = Stopwatch.StartNew();

                            T3.Core.Utils.Utilities.CopyImageMemory(videoFrame.p_data, dataBox.DataPointer, yres, videoFrame.line_stride_in_bytes, dataBox.RowPitch);

                            // release our resources
                            immediateContext.UnmapSubresource(writableImage, 0);
                            Texture.Value = writableImage;
                            UploadTime.Value = (float)sw.Elapsed.TotalMilliseconds;

                            // free frames that were received after use
                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);

                        });

                        _textureMutex.ReleaseMutex();

                        break;

                    // audio is beyond the scope of this example
                    case NDIlib.frame_type_e.frame_type_audio:

                        // if no audio or disabled, nothing to do
                        if (!_audioEnabled || audioFrame.p_data == IntPtr.Zero || audioFrame.no_samples == 0)
                        {
                            // alreays free received frames
                            NDIlib.recv_free_audio_v2(_recvInstancePtr, ref audioFrame);

                            break;
                        }

                        // if the audio format changed, we need to reconfigure the audio device
                        bool formatChanged = false;

                        // make sure our format has been created and matches the incomming audio
                        if (_waveFormat == null ||
                            _waveFormat.Channels != audioFrame.no_channels ||
                            _waveFormat.SampleRate != audioFrame.sample_rate)
                        {
                            // Create a wavformat that matches the incomming frames
                            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)audioFrame.sample_rate, (int)audioFrame.no_channels);

                            formatChanged = true;
                        }

                        // set up our audio buffer if needed
                        if (_bufferedProvider == null || formatChanged)
                        {
                            _bufferedProvider = new BufferedWaveProvider(_waveFormat);
                            _bufferedProvider.DiscardOnBufferOverflow = true;
                        }

                        // set up our multiplexer used to mix down to 2 output channels)
                        if (_multiplexProvider == null || formatChanged)
                        {
                            _multiplexProvider = new MultiplexingWaveProvider(new List<IWaveProvider>() { _bufferedProvider }, 2);
                        }


                        // set up our audio output device
                        if (_haveAudioDevice && (_wasapiOut == null || formatChanged))
                        {
                            try
                            {
                                // We can't guarantee audio sync or buffer fill, that's beyond the scope of this example.
                                // This is close enough to show that audio is received and converted correctly.
                                _wasapiOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 50);
                                _wasapiOut.Init(_multiplexProvider);
                                //_wasapiOut.Volume = _volume;
                                _wasapiOut.Play();
                            }
                            catch
                            {
                                // if this fails, assume that there is no audio device on the system
                                // so that we don't retry/catch on every audio frame received
                                _haveAudioDevice = false;
                            }
                        }

                        // did we get a device?
                        if (_haveAudioDevice && _wasapiOut != null)
                        {
                            // we're working in bytes, so take the size of a 32 bit sample (float) into account
                            int sizeInBytes = (int)audioFrame.no_samples * (int)audioFrame.no_channels * sizeof(float);

                            // NAudio is expecting interleaved audio and NDI uses planar.
                            // create an interleaved frame and convert from the one we received
                            NDIlib.audio_frame_interleaved_32f_t interleavedFrame = new NDIlib.audio_frame_interleaved_32f_t()
                            {
                                sample_rate = audioFrame.sample_rate,
                                no_channels = audioFrame.no_channels,
                                no_samples = audioFrame.no_samples,
                                timecode = audioFrame.timecode
                            };

                            // we need a managed byte array to add to buffered provider
                            byte[] audBuffer = new byte[sizeInBytes];

                            // pin the byte[] and get a GC handle to it
                            // doing it this way saves an expensive Marshal.Alloc/Marshal.Copy/Marshal.Free later
                            // the data will only be moved once, during the fast interleave step that is required anyway
                            GCHandle handle = GCHandle.Alloc(audBuffer, GCHandleType.Pinned);

                            // access it by an IntPtr and use it for our interleaved audio buffer
                            interleavedFrame.p_data = handle.AddrOfPinnedObject();

                            // Convert from float planar to float interleaved audio
                            // There is a matching version of this that converts to interleaved 16 bit audio frames if you need 16 bit
                            NDIlib.util_audio_to_interleaved_32f_v2(ref audioFrame, ref interleavedFrame);

                            // release the pin on the byte[]
                            // never try to access p_data after the byte[] has been unpinned!
                            // that IntPtr will no longer be valid.
                            handle.Free();

                            // push the byte[] buffer into the bufferedProvider for output
                            _bufferedProvider.AddSamples(audBuffer, 0, sizeInBytes);
                        }

                        // free the frame that was received
                        NDIlib.recv_free_audio_v2(_recvInstancePtr, ref audioFrame);

                        break;
                    // Metadata
                    case NDIlib.frame_type_e.frame_type_metadata:

                        // UTF-8 strings must be converted for use - length includes the terminating zero
                        //String metadata = Utf8ToString(metadataFrame.p_data, metadataFrame.length-1);

                        //System.Diagnostics.Debug.Print(metadata);

                        // free frames that were received
                        NDIlib.recv_free_metadata(_recvInstancePtr, ref metadataFrame);
                        break;
                }
            }
        }

        protected void DisposeTextures()
        {
            Texture.Value = null;

            foreach (var image in ImagesWithCpuAccess)
                image?.Dispose();

            ImagesWithCpuAccess.Clear();
        }

        #region IDisposable Support

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // This call happens when the window is closing, so we set the
                    // Id to 0 to signal we don't want to process any more frames.
                    Interlocked.Exchange(ref _receiverId, 0);

                    // tell the thread to exit
                    _exitThread = true;

                    // wait for it to exit
                    if (_receiveThread != null)
                    {
                        _receiveThread.Join();
                        _receiveThread = null;
                    }

                    // Stop the audio device if needed
                    if (_wasapiOut != null)
                    {
                        _wasapiOut.Stop();
                        _wasapiOut.Dispose();
                        _wasapiOut = null;
                    }
                }

                // Destroy the receiver
                if (_recvInstancePtr != IntPtr.Zero)
                {
                    NDIlib.recv_destroy(_recvInstancePtr);
                    _recvInstancePtr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }
        public new void Dispose()
        {
            Dispose(true);

            // dispose textures
            DisposeTextures();

            if (_findInstance != null)
                _findInstance.Dispose();

            if (_initialized)
            {
                NDIlib.destroy();
                _initialized = false;
            }
        }

        #endregion

        private static bool _initialized;               // were static members initialized?
        private Finder _findInstance;
        private bool _disposed = false;

        // a pointer to our unmanaged NDI receiver instance
        IntPtr _recvInstancePtr = IntPtr.Zero;

        // a thread to receive frames on so that the UI is still functional
        Thread _receiveThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;

        // should we send audio to Windows or not?
        private bool _audioEnabled = false;

        // should we send video to Windows or not?
        private bool _videoEnabled = true;

        // the NAudio related
        private WasapiOut _wasapiOut = null;
        private bool _haveAudioDevice = true;
        private MultiplexingWaveProvider _multiplexProvider = null;
        private BufferedWaveProvider _bufferedProvider = null;

        // The last WaveFormat we used.
        // This may change over time, so remember how we are configured currently.
        private WaveFormat _waveFormat = null;

        // the current audio volume
        //private float _volume = 1.0f;

        //private bool _isPtz = false;
        //private bool _canRecord = false;
        //private String _webControlUrl = String.Empty;
        private String _sourceName = String.Empty;

        // This variable keeps track of the current Id of the receiver object. This
        // is a way to avoid processing frames on the UI thread when either the
        // connection source gets changed or the window closes.
        private int _receiverId = 0;

        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;
        private readonly List<Texture2D> ImagesWithCpuAccess = new();
        // current image index (used for circular access of ImagesWithCpuAccess)
        private int _currentIndex;
        // mutex protecting changes to our images
        private readonly Mutex _textureMutex = new();
        // action for changing the textures
        private Action _textureAction;

        private enum SourceIndex
        {
            Source0 = 0,
            Source1,
            Source2,
            Source3,
            Source4,
            Source5,
            Source6,
            Source7,
            Source8,
            Source9
        };

        [Input(Guid = "306668F7-881D-4AEB-9AA0-0460013A05CB")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "D2C24F47-4B24-4037-8B1C-A85378359D2D", MappedType = typeof(SourceIndex))]
        public InputSlot<int> SourceNumber = new();

        [Input(Guid = "FD1FCA6B-A3BE-440B-86BA-6B7B1BBD2A8C")]
        public InputSlot<string> SourceName = new();
    }
}
