using NAudio.Wave;
using NewTek;
using NewTek.NDI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NewTek.NDI.WPF
{
    // If you do not use this control, you can remove this file
    // and remove the dependency on naudio.
    // Alternatively you can also remove any naudio related entries
    // and use it for video only, but don't forget that you will still need
    // to free any audio frames received.
    public class ReceiveView : Viewbox, IDisposable, INotifyPropertyChanged
    {
        [Category("NewTek NDI"),
        Description("The name of this receiver channel. Required or else an invalid argument exception will be thrown.")]
        public String ReceiverName
        {
            get { return (String)GetValue(ReceiverNameProperty); }
            set { SetValue(ReceiverNameProperty, value); }
        }
        public static readonly DependencyProperty ReceiverNameProperty =
            DependencyProperty.Register("ReceiverName", typeof(String), typeof(ReceiveView), new PropertyMetadata(""));



        [Category("NewTek NDI"),
        Description("The NDI source to connect to. An empty new Source() or a Source with no Name will disconnect.")]
        public Source ConnectedSource
        {
            get { return (Source)GetValue(ConnectedSourceProperty); }
            set { SetValue(ConnectedSourceProperty, value); }
        }
        public static readonly DependencyProperty ConnectedSourceProperty =
            DependencyProperty.Register("ConnectedSource", typeof(Source), typeof(ReceiveView), new PropertyMetadata(new Source(), OnConnectedSourceChanged));


        [Category("NewTek NDI"),
        Description("If true (default) received audio will be sent to the default Windows audio playback device.")]
        public bool IsAudioEnabled
        {
            get { return _audioEnabled; }
            set
            {
                if (value != _audioEnabled)
                {
                    NotifyPropertyChanged("IsAudioEnabled");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("If true (default) received video will be sent to the screen.")]
        public bool IsVideoEnabled
        {
            get { return _videoEnabled; }
            set
            {
                if (value != _videoEnabled)
                {
                    NotifyPropertyChanged("IsVideoEnabled");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("Set or get the current audio volume. Range is 0.0 to 1.0")]
        public float Volume
        {
            get { return _volume; }
            set
            {
                if (value != _volume)
                {
                    _volume = Math.Max(0.0f, Math.Min(1.0f, value));

                    if (_wasapiOut != null)
                        _wasapiOut.Volume = _volume;

                    NotifyPropertyChanged("Volume");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("Does the current source support PTZ functionality?")]
        public bool IsPtz
        {
            get { return _isPtz; }
            set
            {
                if (value != _isPtz)
                {
                    NotifyPropertyChanged("IsPtz");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("Does the current source support record functionality?")]
        public bool IsRecordingSupported
        {
            get { return _canRecord; }
            set
            {
                if (value != _canRecord)
                {
                    NotifyPropertyChanged("IsRecordingSupported");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("The web control URL for the current device, as a String, or an Empty String if not supported.")]
        public String WebControlUrl
        {
            get { return _webControlUrl; }
            set
            {
                if (value != _webControlUrl)
                {
                    NotifyPropertyChanged("WebControlUrl");
                }
            }
        }

        public ReceiveView()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region PTZ Methods
        public bool SetPtzZoom(double value)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_zoom(_recvInstancePtr, (float)value);
        }

        public bool SetPtzZoomSpeed(double value)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_zoom_speed(_recvInstancePtr, (float)value);
        }

        public bool SetPtzPanTilt(double pan, double tilt)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_pan_tilt(_recvInstancePtr, (float)pan, (float)tilt);
        }

        public bool SetPtzPanTiltSpeed(double panSpeed, double tiltSpeed)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_pan_tilt_speed(_recvInstancePtr, (float)panSpeed, (float)tiltSpeed);
        }

        public bool PtzStorePreset(int index)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero || index < 0 || index > 99)
                return false;

            return NDIlib.recv_ptz_store_preset(_recvInstancePtr, index);
        }

        public bool PtzRecallPreset(int index, double speed)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero || index < 0 || index > 99)
                return false;

            return NDIlib.recv_ptz_recall_preset(_recvInstancePtr, index, (float)speed);
        }

        public bool PtzAutoFocus()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_auto_focus(_recvInstancePtr);
        }

        public bool SetPtzFocusSpeed(double speed)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_focus_speed(_recvInstancePtr, (float)speed);
        }

        public bool PtzWhiteBalanceAuto()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_auto(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceIndoor()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_indoor(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceOutdoor()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_outdoor(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceOneShot()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_oneshot(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceManual(double red, double blue)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_manual(_recvInstancePtr, (float)red, (float)blue);
        }

        public bool PtzExposureAuto()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_exposure_auto(_recvInstancePtr);
        }

        public bool PtzExposureManual(double level)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_exposure_manual(_recvInstancePtr, (float)level);
        }

        #endregion PTZ Methods

        #region Recording Methods
        // This will start recording.If the recorder was already recording then the message is ignored.A filename is passed in as a ‘hint’.Since the recorder might 
        // already be recording(or might not allow complete flexibility over its filename), the filename might or might not be used.If the filename is empty, or 
        // not present, a name will be chosen automatically. 
        public bool RecordingStart(String filenameHint = "")
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            bool retVal = false;

            if (String.IsNullOrEmpty(filenameHint))
            {
                retVal = NDIlib.recv_recording_start(_recvInstancePtr, IntPtr.Zero);
            }
            else
            {
                // convert to an unmanaged UTF8 IntPtr
                IntPtr fileNamePtr = NDI.UTF.StringToUtf8(filenameHint);

                retVal = NDIlib.recv_recording_start(_recvInstancePtr, IntPtr.Zero);

                // don't forget to free it
                Marshal.FreeHGlobal(fileNamePtr);
            }

            return retVal;
        }

        // Stop recording.
        public bool RecordingStop()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_stop(_recvInstancePtr);
        }


        public bool RecordingSetAudioLevel(double level)
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_set_audio_level(_recvInstancePtr, (float)level);
        }

        public bool IsRecording()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_is_recording(_recvInstancePtr);
        }

        public String GetRecordingFilename()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return String.Empty;

            IntPtr filenamePtr = NDIlib.recv_recording_get_filename(_recvInstancePtr);
            if (filenamePtr == IntPtr.Zero)
            {
                return String.Empty;
            }
            else
            {
                String filename = NDI.UTF.Utf8ToString(filenamePtr);

                // free it
                NDIlib.recv_free_string(_recvInstancePtr, filenamePtr);

                return filename;
            }
        }

        public String GetRecordingError()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return String.Empty;

            IntPtr errorPtr = NDIlib.recv_recording_get_error(_recvInstancePtr);
            if (errorPtr == IntPtr.Zero)
            {
                return String.Empty;
            }
            else
            {
                String error = NDI.UTF.Utf8ToString(errorPtr);

                // free it
                NDIlib.recv_free_string(_recvInstancePtr, errorPtr);

                return error;
            }
        }

        public bool GetRecordingTimes(ref NDIlib.recv_recording_time_t recordingTimes)
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_get_times(_recvInstancePtr, ref recordingTimes);
        }

        #endregion Recording Methods

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ReceiveView()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
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

        private bool _disposed = false;

        // when the ConnectedSource changes, connect to it.
        private static void OnConnectedSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ReceiveView s = sender as ReceiveView;
            if (s == null)
                return;

            s.Connect(s.ConnectedSource);
        }

        // connect to an NDI source in our Dictionary by name
        private void Connect(Source source)
        {
            // Increment the receiver Id, meaning we have a new source to work with. If
            // there's already another receiver thread running, the commands it has
            // sent to the UI won't be processed.
            int receiverId = Interlocked.Increment(ref _receiverId);

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            // before we are connected, we need to set up our image
            // it's bad practice to do this in the constructor
            if (Child == null)
                Child = VideoSurface;

            // just to be safe
            Disconnect();

            // Sanity
            if (source == null || String.IsNullOrEmpty(source.Name))
                return;

            if (String.IsNullOrEmpty(ReceiverName))
                throw new ArgumentException("ReceiverName can not be null or empty.", ReceiverName);

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
                p_ndi_recv_name = UTF.StringToUtf8(ReceiverName)
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
            IsPtz = false;
            IsRecordingSupported = false;
            WebControlUrl = String.Empty;
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
                        IsPtz = NDIlib.recv_ptz_is_supported(_recvInstancePtr);

                        // Check for recording
                        IsRecordingSupported = NDIlib.recv_recording_is_supported(_recvInstancePtr);

                        // Check for a web control URL
                        // We must free this string ptr if we get one.
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

                        break;

                    // Video data
                    case NDIlib.frame_type_e.frame_type_video:

                        // if not enabled, just discard
                        // this can also occasionally happen when changing sources
                        if (!_videoEnabled || videoFrame.p_data == IntPtr.Zero)
                        {
                            // alreays free received frames
                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);

                            break;
                        }

                        // get all our info so that we can free the frame
                        int yres = (int)videoFrame.yres;
                        int xres = (int)videoFrame.xres;

                        // quick and dirty aspect ratio correction for non-square pixels - SD 4:3, 16:9, etc.
                        double dpiX = 96.0 * (videoFrame.picture_aspect_ratio / ((double)xres / (double)yres));

                        int stride = (int)videoFrame.line_stride_in_bytes;
                        int bufferSize = yres * stride;

                        // We need to be on the UI thread to write to our bitmap
                        // Not very efficient, but this is just an example
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                        // If the local receiver Id is not the same as the global receiver Id,
                        // then that means that either the connection source has changed, or
                        // the window has closed, in which case the latest receiver Id
                        // will be 0. If either is true, we stop processing data.
                        if (currReceiverId != _receiverId
                                || _receiverId == 0)
                            {
                                return;
                            }

                        // resize the writeable if needed
                        if (VideoBitmap == null ||
                                VideoBitmap.PixelWidth != xres ||
                                VideoBitmap.PixelHeight != yres ||
                                VideoBitmap.DpiX != dpiX)
                            {
                                VideoBitmap = new WriteableBitmap(xres, yres, dpiX, 96.0, PixelFormats.Pbgra32, null);
                                VideoSurface.Source = VideoBitmap;
                            }

                        // update the writeable bitmap
                        VideoBitmap.WritePixels(new Int32Rect(0, 0, xres, yres), videoFrame.p_data, bufferSize, stride);

                        // free frames that were received AFTER use!
                        // This writepixels call is dispatched, so we must do it inside this scope.
                        NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                        }));

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
                                _wasapiOut.Volume = _volume;
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

        // a pointer to our unmanaged NDI receiver instance
        IntPtr _recvInstancePtr = IntPtr.Zero;

        // a thread to receive frames on so that the UI is still functional
        Thread _receiveThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;

        // the image that will show our bitmap source
        private Image VideoSurface = new Image();

        // the bitmap source we copy received frames into
        private WriteableBitmap VideoBitmap;

        // should we send audio to Windows or not?
        private bool _audioEnabled = true;

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
        private float _volume = 1.0f;

        private bool _isPtz = false;
        private bool _canRecord = false;
        private String _webControlUrl = String.Empty;
        private String _receiverName = String.Empty;

        // This variable keeps track of the current Id of the receiver object. This
        // is a way to avoid processing frames on the UI thread when either the
        // connection source gets changed or the window closes.
        private int _receiverId = 0;
    }
}