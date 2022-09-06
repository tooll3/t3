/*

Based on the MIT license video writing example at
https://github.com/jtpgames/Kinect-Recorder/blob/master/KinectRecorder/Multimedia/MediaFoundationVideoWriter.cs

*/

/*
The MIT License (MIT)

Copyright(c) 2016 Juri Tomak

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.MediaFoundation;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;

using MF = SharpDX.MediaFoundation;


namespace T3.Gui.Windows
{
    abstract class MediaFoundationVideoWriter : IDisposable
    {
        private static readonly Guid VIDEO_INPUT_FORMAT = MF.VideoFormatGuids.Rgb32;

        private static bool _MFInitialized = false;

        #region Configuration
        private int _bitrate;
        private int _framerate;
        private bool _supportAudio;
        private Guid _videoInputFormat;
        #endregion

        #region Resources for MediaFoundation video rendering
        // private MF.ByteStream outStream;
        private MF.SinkWriter _sinkWriter = null;
        private string _filePath;
        private SharpDX.Size2 _videoPixelSize;
        private int _frameIndex;
        private int _streamIndex;
        #endregion

        public int StreamIndex => _streamIndex;

        public MF.SinkWriter SinkWriter => _sinkWriter;

        public string FilePath { get { return _filePath; } }

        private MediaFoundationAudioWriter audioWriter;

        private static SinkWriter CreateSinkWriter(string outputFile)
        {
            SinkWriter writer;
            using (var attributes = new MediaAttributes())
            {
                MediaFactory.CreateAttributes(attributes, 1);
                attributes.Set(SinkWriterAttributeKeys.ReadwriteEnableHardwareTransforms.Guid, (UInt32)1);
                try
                {
                    writer = MediaFactory.CreateSinkWriterFromURL(outputFile, null, attributes);
                }
                catch (COMException e)
                {
                    if (e.ErrorCode == unchecked((int)0xC00D36D5))
                    {
                        throw new ArgumentException("Was not able to create a sink writer for this file extension");
                    }
                    throw;
                }
            }
            return writer;
        }

        public MediaFoundationVideoWriter(string filePath, Size2 videoPixelSize)
            : this(filePath, videoPixelSize, VIDEO_INPUT_FORMAT)
        {
        }

        public MediaFoundationVideoWriter(string filePath, Size2 videoPixelSize, Guid videoInputFormat, bool supportAudio = false)
        {
            if (!_MFInitialized)
            {
                // Initialize MF library. MUST be called before any MF related operations.
                MF.MediaFactory.Startup(MF.MediaFactory.Version, 0);
            }

            // Set initial default values
            _filePath = filePath;
            _videoPixelSize = videoPixelSize;
            _videoInputFormat = videoInputFormat;
            _supportAudio = supportAudio;
            _bitrate = 1500000;
            _framerate = 15;
            _frameIndex = -1;
        }

        /// <summary>
        /// get minimum image buffer size in bytes
        /// </summary>
        /// <param name="frame">texture to get information from</param>
        public static int sizeInBytes(Texture2D frame)
        {
            var currentDesc = frame.Description;
            var bitsPerPixel = Math.Max(FormatHelper.SizeOfInBits(currentDesc.Format), 1);
            return (currentDesc.Width * currentDesc.Height * bitsPerPixel + 7) / 8;
        }

        // FIXME: Would possibly need some refactoring not to duplicate code from ScreenshotWriter
        private static float Read2BytesToHalf(DataStream imageStream)
        {
            var low = (byte)imageStream.ReadByte();
            var high = (byte)imageStream.ReadByte();
            return ToTwoByteFloat(low, high);
        }

        // FIXME: Would possibly need some refactoring not to duplicate code from ScreenshotWriter
        public static float ToTwoByteFloat(byte ho, byte lo)
        {
            var intVal = BitConverter.ToInt32(new byte[] { ho, lo, 0, 0 }, 0);

            int mant = intVal & 0x03ff;
            int exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                }
                while ((mant & 0x400) == 0);

                mant &= 0x3ff;
            }

            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
        }

        public MF.Sample CreateSampleFromFrame(Texture2D frame)
        {
            var device = ResourceManager.Instance().Device;
            MF.MediaBuffer mediaBuffer = MF.MediaFactory.CreateMemoryBuffer(sizeInBytes(frame));

            // Write all contents to the MediaBuffer for media foundation
            int cbMaxLength = 0;
            int cbCurrentLength = 0;
            IntPtr mediaBufferPointer = mediaBuffer.Lock(out cbMaxLength, out cbCurrentLength);
            try
            {
                unsafe
                {
                    int stride = _videoPixelSize.Width;
                    int* mediaBufferPointerNative = (int*)mediaBufferPointer.ToPointer();
                    var currentDesc = frame.Description;
                    var imageDesc = new Texture2DDescription
                    {
                        BindFlags = BindFlags.None,
                        Format = currentDesc.Format,
                        Width = currentDesc.Width,
                        Height = currentDesc.Height,
                        MipLevels = currentDesc.MipLevels,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                        Usage = ResourceUsage.Staging,
                        OptionFlags = ResourceOptionFlags.None,
                        CpuAccessFlags = CpuAccessFlags.Read,
                        ArraySize = 1
                    };

                    var ImageWithCpuAccess = new Texture2D(device, imageDesc);
                    var immediateContext = device.ImmediateContext;
                    immediateContext.CopyResource(frame, ImageWithCpuAccess);
                    immediateContext.UnmapSubresource(ImageWithCpuAccess, 0);
                    DataBox dataBox = immediateContext.MapSubresource(ImageWithCpuAccess,
                                                                        0,
                                                                        0,
                                                                        MapMode.Read,
                                                                        SharpDX.Direct3D11.MapFlags.None,
                                                                        out var imageStream);

                    using (imageStream)
                    {
                        int* data = null;
                        if (currentDesc.Format == SharpDX.DXGI.Format.R16G16B16A16_Float)
                        {
                            for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                            {
                                for (int loopX = 0; loopX < _videoPixelSize.Width; loopX++)
                                {
                                    if (!FlipY)
                                        imageStream.Position = (long)(loopY) * dataBox.RowPitch +
                                                               (long)(loopX) * 8;
                                    else
                                        imageStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch +
                                                               (long)(loopX) * 8;

                                    var r = Read2BytesToHalf(imageStream);
                                    var g = Read2BytesToHalf(imageStream);
                                    var b = Read2BytesToHalf(imageStream);
                                    var a = Read2BytesToHalf(imageStream);

                                    UInt32 aPart = (UInt32)(a.Clamp(0, 1) * 255 + 0.5) << 24;
                                    UInt32 bPart = (UInt32)(r.Clamp(0, 1) * 255 + 0.5) << 16;
                                    UInt32 gPart = (UInt32)(g.Clamp(0, 1) * 255 + 0.5) << 8;
                                    UInt32 rPart = (UInt32)(b.Clamp(0, 1) * 255 + 0.5);

                                    int actIndexTarget = (loopY * _videoPixelSize.Width) + loopX;
                                    mediaBufferPointerNative[actIndexTarget] = (int)(rPart | gPart | bPart | aPart);
                                }
                            }
                        }
                        else if (currentDesc.Format == SharpDX.DXGI.Format.R8G8B8A8_UNorm)
                        {
                            for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                            {
                                for (int loopX = 0; loopX < _videoPixelSize.Width; loopX++)
                                {
                                    if (!FlipY)
                                        imageStream.Position = (long)(loopY) * dataBox.RowPitch +
                                                               (long)(loopX) * 4;
                                    else
                                        imageStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch +
                                                               (long)(loopX) * 4;

                                    byte r = (byte)imageStream.ReadByte();
                                    byte g = (byte)imageStream.ReadByte();
                                    byte b = (byte)imageStream.ReadByte();
                                    byte a = (byte)imageStream.ReadByte();

                                    UInt32 aPart = (UInt32)(a) << 24;
                                    UInt32 bPart = (UInt32)(r) << 16;
                                    UInt32 gPart = (UInt32)(g) << 8;
                                    UInt32 rPart = (UInt32)(b);

                                    int actIndexTarget = (loopY * _videoPixelSize.Width) + loopX;
                                    mediaBufferPointerNative[actIndexTarget] = (int)(rPart | gPart | bPart | aPart);
                                }
                            }
                        }
                        else
                        {
                            // unsupported format, return null
                            return null;
                        }
                    }
                }
            }
            finally
            {
                mediaBuffer.Unlock();
                mediaBuffer.CurrentLength = sizeInBytes(frame);
            }

            // Create the sample (includes image and timing information)
            MF.Sample sample = MF.MediaFactory.CreateSample();
            sample.AddBuffer(mediaBuffer);

            return sample;
        }

        public bool AddVideoFrame(Texture2D frame)
        {
            try
            {
                Debug.Assert(frame != null);
                var currentDesc = frame.Description;
                Debug.Assert(currentDesc.Width != 0 &&
                             currentDesc.Height != 0);

                if (currentDesc.Format != SharpDX.DXGI.Format.R16G16B16A16_Float &&
                    currentDesc.Format != SharpDX.DXGI.Format.R8G8B8A8_UNorm)
                {
                    throw new InvalidOperationException("Only R8G8B8A8_UNorm and R16G16B16A16_Float " +
                                                        "input formats are supported so far.");
                }

                if (_sinkWriter == null)
                {
                    // Create the sink writer if it does not exist so far
                    _sinkWriter = CreateSinkWriter(_filePath);
                    _disposedValue = false;
                    CreateMediaTarget(_sinkWriter, _videoPixelSize, out _streamIndex);

                    // Configure media type of video
                    using (MF.MediaType mediaTypeIn = new MF.MediaType())
                    {
                        mediaTypeIn.Set<Guid>(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
                        mediaTypeIn.Set<Guid>(MF.MediaTypeAttributeKeys.Subtype, _videoInputFormat);
                        mediaTypeIn.Set<int>(MF.MediaTypeAttributeKeys.InterlaceMode, (int)MF.VideoInterlaceMode.Progressive);
                        mediaTypeIn.Set<long>(MF.MediaTypeAttributeKeys.FrameSize, MFHelper.GetMFEncodedIntsByValues(_videoPixelSize.Width, _videoPixelSize.Height));
                        mediaTypeIn.Set<long>(MF.MediaTypeAttributeKeys.FrameRate, MFHelper.GetMFEncodedIntsByValues(_framerate, 1));
                        _sinkWriter.SetInputMediaType(_streamIndex, mediaTypeIn, null);
                    }

                    // Create audio support?
                    if (_supportAudio)
                    {
                        // initialize audio writer
                        var waveFormat = WAVEFORMATEX.DefaultPCM;
                        audioWriter = new MP3AudioWriter(_sinkWriter, ref waveFormat);
                    }

                    // Start writing the video file. MUST be called before write operations.
                    _sinkWriter.BeginWriting();
                }
            }
            catch (Exception e)
            {
                _sinkWriter?.Dispose();
                _sinkWriter = null;
                throw new InvalidOperationException(e.ToString() +
                    "(image size may be unsupported with the requested codec)");
            }

            // Create the sample (includes image and timing information)
            var videoSample = CreateSampleFromFrame(frame);
            try
            {
                // Write to stream
                var samples = new Dictionary<int, Sample>();
                samples.Add(StreamIndex, videoSample);

                WriteSamples(samples);
            }
            catch (SharpDXException e)
            {
                Debug.WriteLine(e.Message);
                throw new InvalidOperationException(e.Message);
            }
            finally
            {
                videoSample.Dispose();
            }

            return true;
        }

        public void AddVideoAndAudioFrame(Texture2D frame, byte[] audioFrame)
        {
            Debug.Assert(frame != null);
            var currentDesc = frame.Description;
            Debug.Assert(currentDesc.Width != 0 &&
                         currentDesc.Height != 0 &&
                         audioFrame != null &&
                         audioFrame.Length != 0);

            var videoSample = CreateSampleFromFrame(frame);
            var audioSample = audioWriter.CreateSampleFromFrame(audioFrame);
            try
            {

                var samples = new Dictionary<int, Sample>();
                samples.Add(StreamIndex, videoSample);
                samples.Add(audioWriter.StreamIndex, audioSample);

                WriteSamples(samples);
            }
            finally
            {
                videoSample.Dispose();
                audioSample.Dispose();
            }
        }

        private void WriteSamples(Dictionary<int, Sample> samples)
        {
            ++_frameIndex;

            long frameDuration;
            MediaFactory.FrameRateToAverageTimePerFrame(_framerate, 1, out frameDuration);

            try
            {
                foreach (var item in samples)
                {
                    var streamIndex = item.Key;
                    var sample = item.Value;

                    sample.SampleTime = frameDuration * _frameIndex;
                    sample.SampleDuration = frameDuration;

                    _sinkWriter.WriteSample(streamIndex, sample);
                }
            }
            catch (SharpDXException e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Creates a media target.
        /// </summary>
        /// <param name="sinkWriter">The previously created SinkWriter.</param>
        /// <param name="videoPixelSize">The pixel size of the video.</param>
        /// <param name="streamIndex">The stream index for the new target.</param>
        protected abstract void CreateMediaTarget(MF.SinkWriter sinkWriter, SharpDX.Size2 videoPixelSize, out int streamIndex);

        /// <summary>
        /// Internal use: FlipY during rendering?
        /// </summary>
        protected virtual bool FlipY
        {
            get { return false; }
        }

        public int Bitrate
        {
            get { return _bitrate; }
            set { _bitrate = value; }
        }

        public int Framerate
        {
            get { return _framerate; }
            set { _framerate = value; }
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_sinkWriter != null)
                    {
                        _sinkWriter.NotifyEndOfSegment(_streamIndex);
                        if (_frameIndex > 0)
                        {
                            _sinkWriter.Finalize();
                        }
                        _sinkWriter.Dispose();
                        _sinkWriter = null;
                    }
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    class MP4VideoWriter : MediaFoundationVideoWriter
    {
        private static readonly Guid VIDEO_ENCODING_FORMAT = MF.VideoFormatGuids.H264;

        public MP4VideoWriter(string filePath, Size2 videoPixelSize)
            : base(filePath, videoPixelSize)
        {

        }

        public MP4VideoWriter(string filePath, Size2 videoPixelSize, Guid videoInputFormat, bool supportAudio = false)
            : base(filePath, videoPixelSize, videoInputFormat, supportAudio)
        {

        }

        protected override void CreateMediaTarget(SinkWriter sinkWriter, Size2 videoPixelSize, out int streamIndex)
        {
            using (MF.MediaType mediaTypeOut = new MF.MediaType())
            {
                mediaTypeOut.Set<Guid>(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
                mediaTypeOut.Set<Guid>(MF.MediaTypeAttributeKeys.Subtype, VIDEO_ENCODING_FORMAT);
                mediaTypeOut.Set<int>(MF.MediaTypeAttributeKeys.AvgBitrate, Bitrate);
                mediaTypeOut.Set<int>(MF.MediaTypeAttributeKeys.InterlaceMode, (int)MF.VideoInterlaceMode.Progressive);
                mediaTypeOut.Set<long>(MF.MediaTypeAttributeKeys.FrameSize, MFHelper.GetMFEncodedIntsByValues(videoPixelSize.Width, videoPixelSize.Height));
                mediaTypeOut.Set<long>(MF.MediaTypeAttributeKeys.FrameRate, MFHelper.GetMFEncodedIntsByValues(Framerate, 1));
                sinkWriter.AddStream(mediaTypeOut, out streamIndex);
            }
        }

        /// <summary>
        /// Internal use: FlipY during rendering?
        /// </summary>
        protected override bool FlipY
        {
            get
            {
                return true;
            }
        }
    }

} // namespace
