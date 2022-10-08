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
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.MediaFoundation;
using SharpDX.DXGI;
using SharpDX.WIC;
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
        // skip a certain number of images at the beginning since the
        // final content will only appear after several buffer flips
        public const int SkipImages = 2;
        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;
        private static readonly List<Texture2D> ImagesWithCpuAccess = new();
        private static int _currentIndex;
        private static int _currentUsageIndex;
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
        public static int SizeInBytes(ref Texture2D frame)
        {
            var currentDesc = frame.Description;
            var bitsPerPixel = Math.Max(FormatHelper.SizeOfInBits(currentDesc.Format), 1);
            return (currentDesc.Width * currentDesc.Height * bitsPerPixel + 7) / 8;
        }

        /// <summary>
        /// get minimum image buffer size in bytes if imager is RGBA converted
        /// </summary>
        /// <param name="frame">texture to get information from</param>
        public static int RGBASizeInBytes(ref Texture2D frame)
        {
            var currentDesc = frame.Description;
            var bitsPerPixel = 32;
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

        public MF.Sample CreateSampleFromFrame(ref Texture2D frame)
        {
            if (frame == null)
                return null;

            // Write all contents to the MediaBuffer for media foundation
            MF.MediaBuffer mediaBuffer = MF.MediaFactory.CreateMemoryBuffer(RGBASizeInBytes(ref frame));
            var device = ResourceManager.Device;
            DataStream inputStream = null;
            DataStream outputStream = null;
            try
            {
                // create several textures with a given format with CPU access
                // to be able to read out the initial texture values
                var currentDesc = frame.Description;
                if (ImagesWithCpuAccess.Count == 0
                    || ImagesWithCpuAccess[0].Description.Format != currentDesc.Format
                    || ImagesWithCpuAccess[0].Description.Width != currentDesc.Width
                    || ImagesWithCpuAccess[0].Description.Height != currentDesc.Height
                    || ImagesWithCpuAccess[0].Description.MipLevels != currentDesc.MipLevels)
                {
                    var imageDesc = new Texture2DDescription
                    {
                        BindFlags = BindFlags.None,
                        Format = currentDesc.Format,
                        Width = currentDesc.Width,
                        Height = currentDesc.Height,
                        MipLevels = currentDesc.MipLevels,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Staging,
                        OptionFlags = ResourceOptionFlags.None,
                        CpuAccessFlags = CpuAccessFlags.Read,
                        ArraySize = 1
                    };

                    DisposeTextures();

                    for (int i = 0; i < NumTextureEntries; ++i)
                    {
                        ImagesWithCpuAccess.Add(new Texture2D(device, imageDesc));
                    }
                    _currentIndex = 0;
                    // skip the first two frames since they will only appear
                    // after buffers have been swapped
                    _currentUsageIndex = -SkipImages;
                }

                // copy the original texture to a readable image
                var immediateContext = device.ImmediateContext;
                var readableImage = ImagesWithCpuAccess[_currentIndex];
                immediateContext.CopyResource(frame, readableImage);
                immediateContext.UnmapSubresource(readableImage, 0);
                _currentIndex = (_currentIndex + 1) % NumTextureEntries;

                // don't return first two samples since buffering is not ready yet
                if (_currentUsageIndex++ < 0)
                    return null;

                // map image resource to get a stream we can read from
                DataBox dataBox = immediateContext.MapSubresource(readableImage,
                                                                  0,
                                                                  0,
                                                                  MapMode.Read,
                                                                  SharpDX.Direct3D11.MapFlags.None,
                                                                  out inputStream);
                // Create an 8 bit RGBA output buffer to write to
                int width = currentDesc.Width;
                int height = currentDesc.Height;
                var formatId = PixelFormat.Format32bppRGBA;
                int rowStride = PixelFormat.GetStride(formatId, width);
                var pixelByteCount = PixelFormat.GetStride(formatId, 1);
                var outBufferSize = height * rowStride;
                outputStream = new DataStream(outBufferSize, true, true);

                int cbMaxLength = 0;
                int cbCurrentLength = 0;
                IntPtr mediaBufferPointer = mediaBuffer.Lock(out cbMaxLength, out cbCurrentLength);

                switch (currentDesc.Format)
                {
                    case SharpDX.DXGI.Format.R16G16B16A16_Float:
                        for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                        {
                            if (!FlipY)
                                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                            else
                                inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

                            long outputPosition = (long)(loopY) * rowStride;

                            for (int loopX = 0; loopX < _videoPixelSize.Width; loopX++)
                            {
                                var r = Read2BytesToHalf(inputStream);
                                var g = Read2BytesToHalf(inputStream);
                                var b = Read2BytesToHalf(inputStream);
                                var a = Read2BytesToHalf(inputStream);

                                outputStream.WriteByte((byte)(b.Clamp(0, 1) * 255));
                                outputStream.WriteByte((byte)(g.Clamp(0, 1) * 255));
                                outputStream.WriteByte((byte)(r.Clamp(0, 1) * 255));
                                outputStream.WriteByte((byte)(a.Clamp(0, 1) * 255));
                            }
                        }
                        break;

                    case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                        for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                        {
                            if (!FlipY)
                                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                            else
                                inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

                            for (int loopX = 0; loopX < _videoPixelSize.Width; loopX++)
                            {
                                byte r = (byte)inputStream.ReadByte();
                                byte g = (byte)inputStream.ReadByte();
                                byte b = (byte)inputStream.ReadByte();
                                byte a = (byte)inputStream.ReadByte();

                                outputStream.WriteByte(b);
                                outputStream.WriteByte(g);
                                outputStream.WriteByte(r);
                                outputStream.WriteByte(a);
                            }
                        }
                        break;

                    case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                        for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                        {
                            if (!FlipY)
                                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                            else
                                inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

                            for (int loopX = 0; loopX < _videoPixelSize.Width; loopX++)
                            {
                                inputStream.ReadByte(); byte r = (byte)inputStream.ReadByte();
                                inputStream.ReadByte(); byte g = (byte)inputStream.ReadByte();
                                inputStream.ReadByte(); byte b = (byte)inputStream.ReadByte();
                                inputStream.ReadByte(); byte a = (byte)inputStream.ReadByte();

                                outputStream.WriteByte(b);
                                outputStream.WriteByte(g);
                                outputStream.WriteByte(r);
                                outputStream.WriteByte(a);
                            }
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Can't export unknown texture format {currentDesc.Format}");
                }

                // copy our finished RGBA buffer to the media buffer pointer
                for (int loopY = 0; loopY < height; loopY++)
                {
                    int index = loopY * rowStride;
                    for (int loopX = width; loopX > 0; --loopX)
                    {
                        int value = Marshal.ReadInt32(outputStream.DataPointer, index);
                        Marshal.WriteInt32(mediaBufferPointer, index, value);
                        index += 4;
                    }
                }

                // release our resources
                immediateContext.UnmapSubresource(readableImage, 0);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Internal image copy failed : " + e.ToString());
            }
            finally
            {
                inputStream?.Dispose();
                outputStream?.Dispose();
                mediaBuffer.Unlock();
                mediaBuffer.CurrentLength = RGBASizeInBytes(ref frame);
            }

            // Create the sample (includes image and timing information)
            MF.Sample sample = MF.MediaFactory.CreateSample();
            sample.AddBuffer(mediaBuffer);

            // we don't need the media buffer here anymore, so dispose it
            // (otherwise we will get memory leaks)
            mediaBuffer.Dispose();
            return sample;
        }

        public void AddVideoFrame(ref Texture2D frame)
        {
            try
            {
                if (frame == null)
                {
                    throw new InvalidOperationException("Handed frame was null");
                }
                var currentDesc = frame.Description;
                if (currentDesc.Width == 0 || currentDesc.Height == 0)
                {
                    throw new InvalidOperationException("Empty image handed over");
                }
                if (currentDesc.Format != SharpDX.DXGI.Format.R8G8B8A8_UNorm &&
                    currentDesc.Format != SharpDX.DXGI.Format.R16G16B16A16_UNorm &&
                    currentDesc.Format != SharpDX.DXGI.Format.R16G16B16A16_Float)
                {
                    throw new InvalidOperationException($"Unknown format: {currentDesc.Format.ToString()}. " +
                                                        "Only R8G8B8A8_UNorm, R16G16B16A16_UNorm and R16G16B16A16_Float " +
                                                        "input formats are supported so far.");
                }

                // Create the sink writer if it does not exist so far
                if (_sinkWriter == null)
                {
                    _sinkWriter = CreateSinkWriter(_filePath);
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
            var videoSample = CreateSampleFromFrame(ref frame);
            if (videoSample != null)
            {
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
            }
        }

        public void AddVideoAndAudioFrame(ref Texture2D frame, byte[] audioFrame)
        {
            Debug.Assert(frame != null);
            var currentDesc = frame.Description;
            Debug.Assert(currentDesc.Width != 0 &&
                         currentDesc.Height != 0 &&
                         audioFrame != null &&
                         audioFrame.Length != 0);

            var videoSample = CreateSampleFromFrame(ref frame);
            var audioSample = audioWriter.CreateSampleFromFrame(audioFrame);
            try
            {
                var samples = new Dictionary<int, Sample>();
                samples.Add(StreamIndex, videoSample);
                samples.Add(audioWriter.StreamIndex, audioSample);

                WriteSamples(samples);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw new InvalidOperationException(e.Message);
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

            foreach (var item in samples)
            {
                var streamIndex = item.Key;
                var sample = item.Value;

                sample.SampleTime = frameDuration * _frameIndex;
                sample.SampleDuration = frameDuration;

                _sinkWriter.WriteSample(streamIndex, sample);
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

        protected void DisposeTextures()
        {
            foreach (var image in ImagesWithCpuAccess)
                image.Dispose();

            ImagesWithCpuAccess.Clear();
        }

        #region IDisposable Support

        public void Dispose()
        {
            if (_sinkWriter != null)
            {
                // since we will try to write on shutdown, things can still go wrong
                try
                {
                    _sinkWriter.NotifyEndOfSegment(_streamIndex);
                    if (_frameIndex > 0)
                    {
                        _sinkWriter.Finalize();
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(e.Message);
                }
                finally
                {
                    _sinkWriter.Dispose();
                    _sinkWriter = null;
                }
            }

            // dispose textures too
            DisposeTextures();
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
