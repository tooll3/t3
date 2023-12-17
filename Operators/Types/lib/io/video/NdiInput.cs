using NewTek;
using NewTek.NDI;
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
using System.Diagnostics;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Operator.Interfaces;

namespace T3.Operators.Types.Id_7567c3b0_9d91_40d2_899d_3a95b481d023
{
    public class NdiInput : Instance<NdiInput>,  IStatusProvider, ICustomDropdownHolder
    {
        [Output(Guid = "85F1AF38-074E-475D-94F5-F48079979509", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        [Output(Guid = "85F1AF38-074E-475D-94F5-F48079979545", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> UploadTime = new();

        public NdiInput()
        {
            InitializeNdi();
            
            // Note that this example does see local sources (new Finder(true))
            // This is for ease of testing, but normally is not needed in released products.
            _ndiInputFinder = new Finder(true);
            Texture.UpdateAction = Update;
            UploadTime.UpdateAction = Update;
        }

        ~NdiInput()
        {
            Dispose(false);
        }


        private double _lastUpdateRunTime;
        private void Update(EvaluationContext context)
        {
            _lastUpdateRunTime = Playback.RunTimeInSecs;
            if (SourceName.DirtyFlag.IsDirty)
            {
                var sourceName = SourceName.GetValue(context);
                _textureMutex.WaitOne(Timeout.Infinite);
                DisposeTextures();
                Connect(sourceName);
                _textureMutex.ReleaseMutex();
            }

            _textureMutex.WaitOne(Timeout.Infinite);
            _textureAction?.Invoke();
            _textureAction = null;
            _textureMutex.ReleaseMutex();
        }

        private bool InitializeNdi()
        {
            // Not required, but "correct". (see the SDK documentation)
            if (_initialized)
                return _initialized;

            _initialized = NDIlib.initialize();

            if (_initialized)
                return _initialized;

            // Cannot run NDI. Most likely because the CPU is not sufficient (see SDK documentation).
            // you can check this directly with a call to NDIlib.is_supported_CPU()
            // not sure why, but it's not going to run
            SetErrorMessage(!NDIlib.is_supported_CPU() 
                                ? "CPU unsupported." 
                                : "Cannot run NDI.");

            return false;
        }

        private void SetErrorMessage(string message)
        {
            Log.Error(message, this);
            _lastStatusMessage = message;
        }


        /// <summary>
        ///  connect to an NDI source in our Dictionary by name
        /// </summary>
        // private void Connect(Source source)
        private void Connect(string sourceName)
        {
            // Increment the receiver Id, meaning we have a new source to work with. If
            // there's already another receiver thread running, the commands it has
            // sent to the UI won't be processed.
            var receiverId = Interlocked.Increment(ref _receiverId);
            
            Disconnect();  // just to be safe

            if (string.IsNullOrEmpty(sourceName))
                return;

            var sourceExists = _ndiInputFinder.Sources.Any(s => s.Name == sourceName);
            if (!sourceExists)
            {
                SetErrorMessage($"NDI source {sourceName} not found");
                return;
            }
            
            Log.Debug($"Connecting to {sourceName}...");

            var selectedSourceT = new NDIlib.source_t
                              {
                                  p_ndi_name = UTF.StringToUtf8(sourceName)
                              };

            var receiverDescription = new NDIlib.recv_create_v3_t
                                          {
                                              source_to_connect_to = selectedSourceT,
                                              color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                                              bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                                              allow_video_fields = false,  // let NDIlib deinterlace for us if needed  ?

                                              // The name of the NDI receiver to create. This is a NULL terminated UTF8 string and should be
                                              // the name of receive channel that you have. This is in many ways symetric with the name of
                                              // senders, so this might be "Channel 1" on your system.
                                              p_ndi_recv_name = UTF.StringToUtf8(sourceName)
                                          };

            // Create a new instance connected to this source
            _receiveInstancePtr = NDIlib.recv_create_v3(ref receiverDescription);

            // Free the memory we allocated with StringToUtf8
            Marshal.FreeHGlobal(selectedSourceT.p_ndi_name);
            Marshal.FreeHGlobal(receiverDescription.p_ndi_recv_name);

            System.Diagnostics.Debug.Assert(_receiveInstancePtr != IntPtr.Zero, "Failed to create NDI receive instance.");

            if (_receiveInstancePtr == IntPtr.Zero)
                return;

            // Mark this source as being on program output for tally purposes (but not on preview)
            SetTallyIndicators(true, false);
            
            _receiveThread = new Thread(ReceiveThreadProc) { IsBackground = true, Name = "NdiInputReceiver" };
            _receiveThread.Start(receiverId);
        }

        private void Disconnect()
        {
            // in case we're connected, reset the tally indicators
            SetTallyIndicators(false, false);

            if (_receiveThread != null)
            {
                _exitThread = true;
                _receiveThread.Join(); // wait for it to end
            }

            _receiveThread = null;
            _exitThread = false;

            NDIlib.recv_destroy(_receiveInstancePtr);

            _receiveInstancePtr = IntPtr.Zero;
        }

        private void SetTallyIndicators(bool onProgram, bool onPreview)
        {
            // We need to have a receive instance
            if (_receiveInstancePtr == IntPtr.Zero)
                return;
            
            var tallyStateDescriptor = new NDIlib.tally_t
                                 {
                                     on_program = onProgram,
                                     on_preview = onPreview
                                 };

            NDIlib.recv_set_tally(_receiveInstancePtr, ref tallyStateDescriptor);
        }

        /// <summary>
        /// Receive thread runs though this loop until told to exit
        /// </summary>
        private void ReceiveThreadProc(object param)
        {
            var device = ResourceManager.Device;

            // Here we keep track of the receiver Id used for this thread.
            var currReceiverId = (int)param;

            while (!_exitThread && _receiveInstancePtr != IntPtr.Zero)
            {
                // The descriptors
                var videoFrame = new NDIlib.video_frame_v2_t();
                var audioFrame = new NDIlib.audio_frame_v2_t();
                var metadataFrame = new NDIlib.metadata_frame_t();

                switch (NDIlib.recv_capture_v2(_receiveInstancePtr, ref videoFrame, ref audioFrame, ref metadataFrame, 1000))
                {
                    // No data
                    case NDIlib.frame_type_e.frame_type_none:
                        break;

                    // frame settings - check for extended functionality
                    case NDIlib.frame_type_e.frame_type_status_change:
                        break;

                    // Video data
                    case NDIlib.frame_type_e.frame_type_video:

                        // if not enabled, just discard
                        // this can also occasionally happen when changing sources
                        if (!VideoEnabled || videoFrame.p_data == IntPtr.Zero)
                        {
                            // always free received frame
                            NDIlib.recv_free_video_v2(_receiveInstancePtr, ref videoFrame);
                            break;
                        }
                        if (Playback.RunTimeInSecs - _lastUpdateRunTime > 1 / 30f)
                        {
                            NDIlib.recv_free_video_v2(_receiveInstancePtr, ref videoFrame);
                            _lastStatusMessage= "skipping frame";
                            break;
                        }

                        // get all our info so that we can free the frame
                        var xRes = videoFrame.xres;
                        var yRes = videoFrame.yres;
                        var stride = videoFrame.line_stride_in_bytes;


                        
                        // Try to acquire our texture mutex for some time.
                        // End processing if we shall quit
                        while (!_textureMutex.WaitOne(10))
                        {
                            if (_exitThread)
                            {
                                // Always free received frame
                                NDIlib.recv_free_video_v2(_receiveInstancePtr, ref videoFrame);
                                break;
                            }
                        }
                        



                        // For now, we need to be on the UI thread to fill our bitmap
                        _textureAction = () =>
                                         {
                                             // If the local receiver Id is not the same as the global receiver Id,
                                             // then that means that either the connection source has changed, or
                                             // the window has closed, in which case the latest receiver Id
                                             // will be 0. If either is true, we stop processing data.
                                             if (currReceiverId != _receiverId || _receiverId == 0)
                                             {
                                                 return;
                                             }

                                             if (xRes == 0 || yRes == 0 || stride == 0)
                                             {
                                                 // always free received frames
                                                 NDIlib.recv_free_video_v2(_receiveInstancePtr, ref videoFrame);
                                                 return;
                                             }

                                             // create several textures with a given format with CPU access
                                             // to be able to read out the initial texture values
                                             const Format textureFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
                                             
                                             if (_imagesWithCpuAccess.Count == 0
                                                 || _imagesWithCpuAccess[0].Description.Format != textureFormat
                                                 || _imagesWithCpuAccess[0].Description.Width != xRes
                                                 || _imagesWithCpuAccess[0].Description.Height != yRes
                                                 || _imagesWithCpuAccess[0].Description.MipLevels != 1)
                                             {
                                                 var imageDesc = new Texture2DDescription
                                                                     {
                                                                         BindFlags = BindFlags.ShaderResource,
                                                                         Format = textureFormat,
                                                                         Width = xRes,
                                                                         Height = yRes,
                                                                         MipLevels = 1,
                                                                         SampleDescription = new SampleDescription(1, 0),
                                                                         Usage = ResourceUsage.Dynamic,
                                                                         OptionFlags = ResourceOptionFlags.None,
                                                                         CpuAccessFlags = CpuAccessFlags.Write,
                                                                         ArraySize = 1
                                                                     };

                                                 DisposeTextures();

                                                 Log.Debug($"NDI input wxh = {xRes}x{yRes}, " +
                                                           $"format = {textureFormat} ({textureFormat})");

                                                 for (var i = 0; i < NumTextureEntries; ++i)
                                                 {
                                                     _imagesWithCpuAccess.Add(new Texture2D(device, imageDesc));
                                                 }

                                                 _currentIndex = 0;
                                             }

                                             // copy texture to an internal image
                                             var immediateContext = device.ImmediateContext;
                                             var writableImage = _imagesWithCpuAccess[_currentIndex];
                                             _currentIndex = (_currentIndex + 1) % NumTextureEntries;

                                             // we have to map with a stride that represents multiples of 16 pixels here
                                             // (it is yet unclear why, but works)


                                             // map resource manually using our stride...
                                             var dataBox = immediateContext.MapSubresource(writableImage, 0, 0, MapMode.WriteDiscard,
                                                                                           SharpDX.Direct3D11.MapFlags.None, out int _);

                                             var sw = Stopwatch.StartNew();

                                             T3.Core.Utils.Utilities.CopyImageMemory(videoFrame.p_data, dataBox.DataPointer, yRes,
                                                                                     videoFrame.line_stride_in_bytes, dataBox.RowPitch);

                                             // release our resources
                                             immediateContext.UnmapSubresource(writableImage, 0);
                                             Texture.Value = writableImage;
                                             UploadTime.Value = (float)sw.Elapsed.TotalMilliseconds;

                                             // free frames that were received after use
                                             NDIlib.recv_free_video_v2(_receiveInstancePtr, ref videoFrame);
                                             _lastStatusMessage = null;
                                         };

                        _textureMutex.ReleaseMutex();

                        break;

                    // Ignore audio
                    case NDIlib.frame_type_e.frame_type_audio:
                        break;
                    
                    
                    // Metadata
                    case NDIlib.frame_type_e.frame_type_metadata:

                        // UTF-8 strings must be converted for use - length includes the terminating zero
                        //String metadata = Utf8ToString(metadataFrame.p_data, metadataFrame.length-1);

                        //System.Diagnostics.Debug.Print(metadata);

                        // free frames that were received
                        NDIlib.recv_free_metadata(_receiveInstancePtr, ref metadataFrame);
                        break;
                }
            }
        }

        private void DisposeTextures()
        {
            Texture.Value = null;

            foreach (var image in _imagesWithCpuAccess)
                image?.Dispose();

            _imagesWithCpuAccess.Clear();
        }

        #region IDisposable Support
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            
            if (disposing)
            {
                // This call happens when the window is closing, so we set the
                // Id to 0 to signal we don't want to process any more frames.
                Interlocked.Exchange(ref _receiverId, 0);

                _exitThread = true;

                // wait for it to exit
                if (_receiveThread != null)
                {
                    _receiveThread.Join();
                    _receiveThread = null;
                }
            }

            // Destroy the receiver
            if (_receiveInstancePtr != IntPtr.Zero)
            {
                NDIlib.recv_destroy(_receiveInstancePtr);
                _receiveInstancePtr = IntPtr.Zero;
            }

            _disposed = true;
        }

        // TODO: clarify if this is called.
        public new void Dispose()
        {
            Dispose(true);

            // dispose textures
            DisposeTextures();

            if (_ndiInputFinder != null)
                _ndiInputFinder.Dispose();

            if (_initialized)
            {
                NDIlib.destroy();
                _initialized = false;
            }
        }
        #endregion

        private static bool _initialized; 
        private readonly Finder _ndiInputFinder;
        private bool _disposed;

        // A pointer to our unmanaged NDI receiver instance
        private IntPtr _receiveInstancePtr = IntPtr.Zero;

        // Thread to receive frames on so that the UI is still functional
        private Thread _receiveThread;

        // A way to exit the thread safely
        private bool _exitThread;


        // should we send video to Windows or not?
        private const bool VideoEnabled = true;

        // private string _sourceName = string.Empty;

        // This variable keeps track of the current Id of the receiver object. This
        // is a way to avoid processing frames on the UI thread when either the
        // connection source gets changed or the window closes.
        private int _receiverId = 0;

        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;

        private readonly List<Texture2D> _imagesWithCpuAccess = new();

        // current image index (used for circular access of ImagesWithCpuAccess)
        private int _currentIndex;

        // mutex protecting changes to our images
        private readonly Mutex _textureMutex = new();

        // action for changing the textures
        private Action _textureAction;
        

        [Input(Guid = "FD1FCA6B-A3BE-440B-86BA-6B7B1BBD2A8C")]
        public InputSlot<string> SourceName = new();

        public string GetValueForInput(Guid inputId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetOptionsForInput(Guid inputId)
        {
            throw new NotImplementedException();
        }

        public void HandleResultForInput(Guid inputId, string result)
        {
            throw new NotImplementedException();
        }

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastStatusMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _lastStatusMessage;
        }

        private string _lastStatusMessage;

        #region device dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return SourceName.Value;
        }
        
        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != SourceName.Id)
            {
                yield return "undefined";
                yield break;
            }
        
            foreach (var s in _ndiInputFinder.Sources)
            {
                yield return s.Name;
            }
        }
        
        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            Log.Debug($"Got {result}", this);
            SourceName.SetTypedInputValue(result);
        }
        #endregion
    }
}