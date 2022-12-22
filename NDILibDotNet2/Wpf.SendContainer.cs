using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NewTek.NDI.WPF
{
    public class NdiSendContainer : Viewbox, INotifyPropertyChanged, IDisposable
    {
        [Category("NewTek NDI"),
        Description("NDI output width in pixels. Required.")]
        public int NdiWidth
        {
            get { return (int)GetValue(NdiWidthProperty); }
            set { SetValue(NdiWidthProperty, value); }
        }
        public static readonly DependencyProperty NdiWidthProperty =
            DependencyProperty.Register("NdiWidth", typeof(int), typeof(NdiSendContainer), new PropertyMetadata(1280));

        [Category("NewTek NDI"),
        Description("NDI output height in pixels. Required.")]
        public int NdiHeight
        {
            get { return (int)GetValue(NdiHeightProperty); }
            set { SetValue(NdiHeightProperty, value); }
        }
        public static readonly DependencyProperty NdiHeightProperty =
            DependencyProperty.Register("NdiHeight", typeof(int), typeof(NdiSendContainer), new PropertyMetadata(720));


        [Category("NewTek NDI"),
        Description("NDI output frame rate numerator. Required.")]
        public int NdiFrameRateNumerator
        {
            get { return (int)GetValue(NdiFrameRateNumeratorProperty); }
            set { SetValue(NdiFrameRateNumeratorProperty, value); }
        }
        public static readonly DependencyProperty NdiFrameRateNumeratorProperty =
            DependencyProperty.Register("NdiFrameRateNumerator", typeof(int), typeof(NdiSendContainer), new PropertyMetadata(60000));

        [Category("NewTek NDI"),
        Description("NDI output frame rate denominator. Required.")]
        public int NdiFrameRateDenominator
        {
            get { return (int)GetValue(NdiFrameRateDenominatorProperty); }
            set { SetValue(NdiFrameRateDenominatorProperty, value); }
        }
        public static readonly DependencyProperty NdiFrameRateDenominatorProperty =
            DependencyProperty.Register("NdiFrameRateDenominator", typeof(int), typeof(NdiSendContainer), new PropertyMetadata(1000));


        [Category("NewTek NDI"),
        Description("NDI output name as displayed to receivers. Required.")]
        public String NdiName
        {
            get { return (String)GetValue(NdiNameProperty); }
            set { SetValue(NdiNameProperty, value); }
        }
        public static readonly DependencyProperty NdiNameProperty =
            DependencyProperty.Register("NdiName", typeof(String), typeof(NdiSendContainer), new PropertyMetadata("Unnamed - Fix Me.", OnNdiSenderPropertyChanged));


        [Category("NewTek NDI"),
        Description("NDI groups this sender will belong to. Optional.")]
        public List<String> NdiGroups
        {
            get { return (List<String>)GetValue(NdiGroupsProperty); }
            set { SetValue(NdiGroupsProperty, value); }
        }
        public static readonly DependencyProperty NdiGroupsProperty =
            DependencyProperty.Register("NdiGroups", typeof(List<String>), typeof(NdiSendContainer), new PropertyMetadata(new List<String>(), OnNdiSenderPropertyChanged));


        [Category("NewTek NDI"),
        Description("If clocked to video, NDI will rate limit drawing to the specified frame rate. Defaults to true.")]
        public bool NdiClockToVideo
        {
            get { return (bool)GetValue(NdiClockToVideoProperty); }
            set { SetValue(NdiClockToVideoProperty, value); }
        }
        public static readonly DependencyProperty NdiClockToVideoProperty =
            DependencyProperty.Register("NdiClockToVideo", typeof(bool), typeof(NdiSendContainer), new PropertyMetadata(true, OnNdiSenderPropertyChanged));

        [Category("NewTek NDI"),
        Description("True if some receiver has this source on program out.")]
        public bool IsOnProgram
        {
            get { return (bool)GetValue(IsOnProgramProperty); }
            set { SetValue(IsOnProgramProperty, value); }
        }
        public static readonly DependencyProperty IsOnProgramProperty =
            DependencyProperty.Register("IsOnProgram", typeof(bool), typeof(NdiSendContainer), new PropertyMetadata(false));

        [Category("NewTek NDI"),
        Description("True if some receiver has this source on preview out.")]
        public bool IsOnPreview
        {
            get { return (bool)GetValue(IsOnPreviewProperty); }
            set { SetValue(IsOnPreviewProperty, value); }
        }
        public static readonly DependencyProperty IsOnPreviewProperty =
            DependencyProperty.Register("IsOnPreview", typeof(bool), typeof(NdiSendContainer), new PropertyMetadata(false));


        [Category("NewTek NDI"),
        Description("If True, the send thread does not send, taking no CPU time.")]
        public bool IsSendPaused
        {
            get { return isPausedValue; }
            set
            {
                if (value != isPausedValue)
                {
                    isPausedValue = value;
                    NotifyPropertyChanged("IsSendPaused");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("If you need partial transparency, set this to true. If not, set to false and save some CPU cycles.")]
        public bool UnPremultiply
        {
            get { return unPremultiply; }
            set
            {
                if (value != unPremultiply)
                {
                    unPremultiply = value;
                    NotifyPropertyChanged("UnPremultiply");
                }
            }
        }

        [Category("NewTek NDI"),
        Description("If you need partial transparency, set this to true. If not, set to false and save some CPU cycles.")]
        public bool SendSystemAudio
        {
            get { return sendSystemAudio; }
            set
            {
                if (value != sendSystemAudio)
                {
                    if (value)
                    {
                        try
                        {
                            audioCap = new WasapiLoopbackCapture();
                            audioCap.StartRecording();
                            audioSampleRate = audioCap.WaveFormat.SampleRate;
                            audioSampleSizeInBytes = audioCap.WaveFormat.BitsPerSample / 8;
                            audioNumChannels = audioCap.WaveFormat.Channels;

                            audioCap.DataAvailable += AudioCap_DataAvailable;
                        }
                        catch
                        {
                            // loopback capture may not be available on all systems
                            value = false;
                        }
                    }
                    else
                    {
                        if (audioCap != null)
                        {
                            if (audioCap.CaptureState == NAudio.CoreAudioApi.CaptureState.Capturing)
                            {
                                audioCap.StopRecording();

                                while (audioCap.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
                                {
                                    Thread.Sleep(10);
                                }
                            }

                            audioCap.Dispose();
                            audioCap = null;
                        }
                    }

                    sendSystemAudio = value;
                    NotifyPropertyChanged("SendSystemAudio");
                }
            }
        }

        private void AudioCap_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (isPausedValue || sendInstancePtr == IntPtr.Zero)
                return;

            // how many samples?
            int numSamples = (e.BytesRecorded / (audioNumChannels * audioSampleSizeInBytes));

            // pin the byte[] audio received and get a GC handle to it
            GCHandle interleavedHandle = GCHandle.Alloc(e.Buffer, GCHandleType.Pinned);

            if (audioSampleSizeInBytes == 2)
            {
                // make an temporary interleaved NDI audio frame around the received samples
                NDIlib.audio_frame_interleaved_16s_t interleavedShortFrame = new NDIlib.audio_frame_interleaved_16s_t()
                {
                    sample_rate = audioSampleRate,
                    no_channels = audioNumChannels,
                    no_samples = numSamples,
                    p_data = interleavedHandle.AddrOfPinnedObject()
                };

                sendInstanceLock.EnterReadLock();

                // Send the interleaved frame.
                if (sendInstancePtr != IntPtr.Zero && !IsSendPaused)
                    NDIlib.util_send_send_audio_interleaved_16s(sendInstancePtr, ref interleavedShortFrame);

                sendInstanceLock.ExitReadLock();
            }
            else if (audioSampleSizeInBytes == 4)
            {
                // make an temporary interleaved NDI audio frame around the received samples
                NDIlib.audio_frame_interleaved_32f_t interleavedFloatFrame = new NDIlib.audio_frame_interleaved_32f_t()
                {
                    sample_rate = audioSampleRate,
                    no_channels = audioNumChannels,
                    no_samples = numSamples,
                    p_data = interleavedHandle.AddrOfPinnedObject()
                };

                sendInstanceLock.EnterReadLock();

                // Send the interleaved frame.
                if (sendInstancePtr != IntPtr.Zero && !IsSendPaused)
                    NDIlib.util_send_send_audio_interleaved_32f(sendInstancePtr, ref interleavedFloatFrame);

                sendInstanceLock.ExitReadLock();
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "Unexpected audio sample size.");
            }

            // release the GC pinning of the byte[]'s
            interleavedHandle.Free();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public NdiSendContainer()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            // start up a thread to receive on
            sendThread = new Thread(SendThreadProc) { IsBackground = true, Name = "WpfNdiSendThread" };
            sendThread.Start();

            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NdiSendContainer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // clean up the audio capture if needed
                if (audioCap != null)
                {
                    audioCap.StopRecording();

                    // have to let it stop
                    while (audioCap.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
                    {
                        Thread.Sleep(10);
                    }

                    audioCap.Dispose();
                    audioCap = null;
                }

                if (disposing)
                {
                    // tell the thread to exit
                    exitThread = true;

                    // wait for it to exit
                    if (sendThread != null)
                    {
                        sendThread.Join();

                        sendThread = null;
                    }

                    // cause the pulling of frames to fail
                    pendingFrames.CompleteAdding();

                    // clear any pending frames
                    while (pendingFrames.Count > 0)
                    {
                        NDIlib.video_frame_v2_t discardFrame = pendingFrames.Take();
                        Marshal.FreeHGlobal(discardFrame.p_data);
                    }

                    pendingFrames.Dispose();
                }

                // Destroy the NDI sender
                if (sendInstancePtr != IntPtr.Zero)
                {
                    NDIlib.send_destroy(sendInstancePtr);
                    sendInstancePtr = IntPtr.Zero;
                }

                if (sendInstanceLock != null)
                {
                    sendInstanceLock.Dispose();
                    sendInstanceLock = null;
                }

                _disposed = true;
            }
        }

        private bool _disposed = false;

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (IsSendPaused)
                return;

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            int xres = NdiWidth;
            int yres = NdiHeight;

            int frNum = NdiFrameRateNumerator;
            int frDen = NdiFrameRateDenominator;

            // sanity
            if (sendInstancePtr == IntPtr.Zero || xres < 8 || yres < 8)
                return;

            if (targetBitmap == null || targetBitmap.PixelWidth != xres || targetBitmap.PixelHeight != yres)
            {
                // Create a properly sized RenderTargetBitmap
                targetBitmap = new RenderTargetBitmap(xres, yres, 96, 96, PixelFormats.Pbgra32);

                fmtConvertedBmp = new FormatConvertedBitmap();
                fmtConvertedBmp.BeginInit();
                fmtConvertedBmp.Source = targetBitmap;
                fmtConvertedBmp.DestinationFormat = PixelFormats.Bgra32;
                fmtConvertedBmp.EndInit();
            }

            // clear to prevent trails
            targetBitmap.Clear();

            // render the content into the bitmap
            targetBitmap.Render(this.Child);

            stride = (xres * 32/*BGRA bpp*/ + 7) / 8;
            bufferSize = yres * stride;
            aspectRatio = (float)xres / (float)yres;

            // allocate some memory for a video buffer
            IntPtr bufferPtr = Marshal.AllocHGlobal(bufferSize);

            // We are going to create a progressive frame at 60Hz.
            NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t()
            {
                // Resolution
                xres = NdiWidth,
                yres = NdiHeight,
                // Use BGRA video
                FourCC = NDIlib.FourCC_type_e.FourCC_type_BGRA,
                // The frame-eate
                frame_rate_N = frNum,
                frame_rate_D = frDen,
                // The aspect ratio
                picture_aspect_ratio = aspectRatio,
                // This is a progressive frame
                frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
                // Timecode.
                timecode = NDIlib.send_timecode_synthesize,
                // The video memory used for this frame
                p_data = bufferPtr,
                // The line to line stride of this image
                line_stride_in_bytes = stride,
                // no metadata
                p_metadata = IntPtr.Zero,
                // only valid on received frames
                timestamp = 0
            };

            if (UnPremultiply && fmtConvertedBmp != null)
            {
                fmtConvertedBmp.CopyPixels(new Int32Rect(0, 0, xres, yres), bufferPtr, bufferSize, stride);
            }
            else
            {
                // copy the pixels into the buffer
                targetBitmap.CopyPixels(new Int32Rect(0, 0, xres, yres), bufferPtr, bufferSize, stride);
            }

            // add it to the output queue
            AddFrame(videoFrame);
        }

        private static void OnNdiSenderPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NdiSendContainer s = sender as NdiSendContainer;
            if (s != null)
                s.InitializeNdi();
        }

        private void InitializeNdi()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            sendInstanceLock.EnterWriteLock();
            {
                // we need a name
                if (String.IsNullOrEmpty(NdiName))
                {
                    sendInstanceLock.ExitWriteLock();
                    return;
                }

                // re-initialize?
                if (sendInstancePtr != IntPtr.Zero)
                {
                    NDIlib.send_destroy(sendInstancePtr);
                    sendInstancePtr = IntPtr.Zero;
                }

                // .Net interop doesn't handle UTF-8 strings, so do it manually
                // These must be freed later
                IntPtr sourceNamePtr = UTF.StringToUtf8(NdiName);

                IntPtr groupsNamePtr = IntPtr.Zero;

                // build a comma separated list of groups?
                if (NdiGroups.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < NdiGroups.Count(); i++)
                    {
                        sb.Append(NdiGroups[i]);

                        if (i < NdiGroups.Count - 1)
                            sb.Append(',');
                    }

                    groupsNamePtr = UTF.StringToUtf8(sb.ToString());
                }

                // Create an NDI source description using sourceNamePtr and it's clocked to the video.
                NDIlib.send_create_t createDesc = new NDIlib.send_create_t()
                {
                    p_ndi_name = sourceNamePtr,
                    p_groups = groupsNamePtr,
                    clock_video = NdiClockToVideo,
                    clock_audio = false
                };

                // We create the NDI finder instance
                sendInstancePtr = NDIlib.send_create(ref createDesc);

                // free the strings we allocated
                Marshal.FreeHGlobal(sourceNamePtr);
                Marshal.FreeHGlobal(groupsNamePtr);
            }
            sendInstanceLock.ExitWriteLock();
        }

        // the receive thread runs though this loop until told to exit
        private void SendThreadProc()
        {
            // look for changes in tally
            bool lastProg = false;
            bool lastPrev = false;

            NDIlib.tally_t tally = new NDIlib.tally_t();
            tally.on_program = lastProg;
            tally.on_preview = lastPrev;

            while (!exitThread)
            {

                if (sendInstanceLock.TryEnterReadLock(0))
                {
                    // if this is not here, then we must be being reconfigured
                    if (sendInstancePtr == null)
                    {
                        // unlock
                        sendInstanceLock.ExitReadLock();

                        // give up some time
                        Thread.Sleep(20);

                        // loop again
                        continue;
                    }

                    try
                    {
                        // get the next available frame
                        NDIlib.video_frame_v2_t frame;
                        if (pendingFrames.TryTake(out frame, 250))
                        {
                            // this dropps frames if the UI is rendernig ahead of the specified NDI frame rate
                            while (pendingFrames.Count > 1)
                            {
                                NDIlib.video_frame_v2_t discardFrame = pendingFrames.Take();
                                Marshal.FreeHGlobal(discardFrame.p_data);
                            }

                            // We now submit the frame. Note that this call will be clocked so that we end up submitting 
                            // at exactly the requested rate.
                            // If WPF can't keep up with what you requested of NDI, then it will be sent at the rate WPF is rendering.
                            if (!IsSendPaused)
                            {
                                NDIlib.send_send_video_v2(sendInstancePtr, ref frame);
                            }

                            // free the memory from this frame
                            Marshal.FreeHGlobal(frame.p_data);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        pendingFrames.CompleteAdding();
                    }
                    catch
                    {
                    }

                    // unlock
                    sendInstanceLock.ExitReadLock();
                }
                else
                {
                    Thread.Sleep(20);
                }

                // check tally
                NDIlib.send_get_tally(sendInstancePtr, ref tally, 0);

                // if tally changed trigger an update
                if (lastProg != tally.on_program || lastPrev != tally.on_preview)
                {
                    // save the last values
                    lastProg = tally.on_program;
                    lastPrev = tally.on_preview;

                    // set these on the UI thread
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        IsOnProgram = lastProg;
                        IsOnPreview = lastPrev;
                    }));
                }
            }
        }

        public bool AddFrame(NDIlib.video_frame_v2_t frame)
        {
            try
            {
                pendingFrames.Add(frame);
            }
            catch (OperationCanceledException)
            {
                // we're shutting down
                pendingFrames.CompleteAdding();
                return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private ReaderWriterLockSlim sendInstanceLock = new ReaderWriterLockSlim();
        private IntPtr sendInstancePtr = IntPtr.Zero;

        RenderTargetBitmap targetBitmap = null;
        FormatConvertedBitmap fmtConvertedBmp = null;

        private int stride;
        private int bufferSize;
        private float aspectRatio;

        // a thread to send frames on so that the UI isn't dragged down
        Thread sendThread = null;

        // a way to exit the thread safely
        bool exitThread = false;

        // a thread safe collection to store pending frames
        BlockingCollection<NDIlib.video_frame_v2_t> pendingFrames = new BlockingCollection<NDIlib.video_frame_v2_t>();

        // used for pausing the send thread
        bool isPausedValue = false;

        // a safe value at the expense of CPU cycles
        bool unPremultiply = true;

        // should we send system audio with the video?
        bool sendSystemAudio = false;

        // a capture device to grab system audio
        WasapiLoopbackCapture audioCap = null;

        // basic description of the audio stream
        int audioSampleRate = 48000;
        int audioSampleSizeInBytes = 4;
        int audioNumChannels = 2;
    }
}
