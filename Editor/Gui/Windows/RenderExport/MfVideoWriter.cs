/*

Based on the MIT license video writing example at
https://github.com/jtpgames/Kinect-Recorder/blob/master/KinectRecorder/Multimedia/MediaFoundationVideoWriter.cs
Copyright(c) 2016 Juri Tomak
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using SharpDX.WIC;
using T3.Core.DataTypes.Vector;
using T3.Core.Resource;
using T3.Core.Utils;
using MF = SharpDX.MediaFoundation;

namespace T3.Editor.Gui.Windows.RenderExport;


internal abstract class MfVideoWriter : IDisposable
{
    /** Skip a certain number of images at the beginning since the
     * final content will only appear after several buffer flips*/
    public const int SkipImages = 0;

    public static List<Format> SupportedFormats { get; }= new()
                                                                       {
                                                                           SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                                                                           SharpDX.DXGI.Format.R16G16B16A16_UNorm,
                                                                           SharpDX.DXGI.Format.R16G16B16A16_Float,
                                                                           SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                                                       };
    
    public string FilePath { get; }
    
    protected MfVideoWriter(string filePath, Int2 videoPixelSize)
        : this(filePath, videoPixelSize, _videoInputFormatId)
    {
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

            if(!SupportedFormats.Contains(currentDesc.Format))
            {
                throw new InvalidOperationException($"Unknown format: {currentDesc.Format.ToString()}. " +
                                                    "Only " + string.Join(", ",SupportedFormats) + " are supported so far.");
            }

            if (SinkWriter == null)
            {
                SinkWriter = CreateSinkWriter(FilePath);
                CreateMediaTarget(SinkWriter, _videoPixelSize, out _streamIndex);

                // Configure media type of video
                using (var mediaTypeIn = new MF.MediaType())
                {
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.Subtype, _videoInputFormat);
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.InterlaceMode, (int)MF.VideoInterlaceMode.Progressive);
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.FrameSize, MfHelper.GetMfEncodedIntsByValues(_videoPixelSize.Width, _videoPixelSize.Height));
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.FrameRate, MfHelper.GetMfEncodedIntsByValues(Framerate, 1));
                    SinkWriter.SetInputMediaType(_streamIndex, mediaTypeIn, null);
                }

                if (_supportAudio)
                {
                    // initialize audio writer
                    var waveFormat = WaveFormatExtension.DefaultPcm;
                    _audioWriter = new Mp3AudioWriter(SinkWriter, ref waveFormat);
                }

                // Start writing the video file. MUST be called before write operations.
                SinkWriter.BeginWriting();
            }
        }
        catch (Exception e)
        {
            SinkWriter?.Dispose();
            SinkWriter = null;
            throw new InvalidOperationException(e +
                                                "(image size may be unsupported with the requested codec)");
        }

        // Create the sample (includes image and timing information)
        var videoSample = CreateSampleFromFrame(ref frame);
        if (videoSample == null)
            return;
        
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


    private MF.Sample CreateSampleFromFrame(ref Texture2D frame)
    {
        if (frame == null)
            return null;

        // Write all contents to the MediaBuffer for media foundation
        var mediaBuffer = MF.MediaFactory.CreateMemoryBuffer(RgbaSizeInBytes(ref frame));
        
        //var device = ResourceManager.Device;
        DataStream inputStream = null;
        DataStream outputStream = null;
        try
        {
            var currentDesc = frame.Description;
            PrepareCpuAccessTextures(currentDesc);

            // Copy the original texture to a readable image
            var immediateContext = ResourceManager.Device.ImmediateContext;
            var readableImage = _imagesWithCpuAccess[_currentIndex];
            immediateContext.CopyResource(frame, readableImage);
            immediateContext.UnmapSubresource(readableImage, 0);
            
            _currentIndex = (_currentIndex + 1) % NumTextureEntries;

            // Don't return first two samples since buffering is not ready yet
            if (_currentUsageIndex++ < 0)
                return null;

            // Map image resource to get a stream we can read from
            var dataBox = immediateContext.MapSubresource(readableImage,
                                                          0,
                                                          0,
                                                          MapMode.Read,
                                                          SharpDX.Direct3D11.MapFlags.None,
                                                          out inputStream);
            // Create an 8 bit RGBA output buffer to write to
            var width = currentDesc.Width;
            var height = currentDesc.Height;
            var formatId = PixelFormat.Format32bppRGBA;
            var rowStride = PixelFormat.GetStride(formatId, width);
            
            //var pixelByteCount = PixelFormat.GetStride(formatId, 1);
            var outBufferSize = height * rowStride;
            outputStream = new DataStream(outBufferSize, true, true);

            var mediaBufferPointer = mediaBuffer.Lock(out _, out _);

            switch (currentDesc.Format)
            {
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    for (var loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                    {
                        if (!FlipY)
                            inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                        else
                            inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

                        //var outputPosition = (long)(loopY) * rowStride;

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

                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    // Note: dataBox.RowPitch and outputStream.RowPitch can diverge if width is not divisible by 16.
                    for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                    {
                        if (!FlipY)
                            inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                        else
                            inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

                        // An attempt to speed up encoding by copying larger ranges. Sadly this froze the execution
                        //inputStream.CopyTo(outputStream, dataBox.RowPitch);

                        outputStream.WriteRange(inputStream.ReadRange<byte>(rowStride));
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
                            inputStream.ReadByte();
                            byte r = (byte)inputStream.ReadByte();
                            inputStream.ReadByte();
                            byte g = (byte)inputStream.ReadByte();
                            inputStream.ReadByte();
                            byte b = (byte)inputStream.ReadByte();
                            inputStream.ReadByte();
                            byte a = (byte)inputStream.ReadByte();

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
            throw new InvalidOperationException("Internal image copy failed : " + e);
        }
        finally
        {
            inputStream?.Dispose();
            outputStream?.Dispose();
            mediaBuffer.Unlock();
            mediaBuffer.CurrentLength = RgbaSizeInBytes(ref frame);
        }

        // Create the sample (includes image and timing information)
        MF.Sample sample = MF.MediaFactory.CreateSample();
        sample.AddBuffer(mediaBuffer);

        // we don't need the media buffer here anymore, so dispose it
        // (otherwise we will get memory leaks)
        mediaBuffer.Dispose();
        return sample;
    }

    private static SinkWriter CreateSinkWriter(string outputFile)
    {
        SinkWriter writer;
        using var attributes = new MediaAttributes();
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

        return writer;
    }
    
    /// <summary>
    /// create several textures with a given format with CPU access to be able to read out the initial texture values
    /// </summary>
    /// <param name="currentDesc"></param>
    private static void PrepareCpuAccessTextures(Texture2DDescription currentDesc)
    {
        if (_imagesWithCpuAccess.Count != 0
            && _imagesWithCpuAccess[0].Description.Format == currentDesc.Format
            && _imagesWithCpuAccess[0].Description.Width == currentDesc.Width
            && _imagesWithCpuAccess[0].Description.Height == currentDesc.Height
            && _imagesWithCpuAccess[0].Description.MipLevels == currentDesc.MipLevels)
            return;
        
        DisposeTextures();
        
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


        for (var i = 0; i < NumTextureEntries; ++i)
        {
            _imagesWithCpuAccess.Add(new Texture2D(ResourceManager.Device, imageDesc));
        }

        _currentIndex = 0;
            
        // skip the first two frames since they will only appear
        // after buffers have been swapped
        _currentUsageIndex = -SkipImages;
    }

    private MfVideoWriter(string filePath, Int2 videoPixelSize, Guid videoInputFormat, bool supportAudio = false)
    {
        if (!_mfInitialized)
        {
            // Initialize MF library. MUST be called before any MF related operations.
            MF.MediaFactory.Startup(MF.MediaFactory.Version, 0);
        }

        // Set initial default values
        FilePath = filePath;
        _videoPixelSize = videoPixelSize;
        _videoInputFormat = videoInputFormat;
        _supportAudio = supportAudio;
        Bitrate = 1500000;
        Framerate = 15;
        _frameIndex = -1;
    }

    /// <summary>
    /// get minimum image buffer size in bytes if imager is RGBA converted
    /// </summary>
    /// <param name="frame">texture to get information from</param>
    private static int RgbaSizeInBytes(ref Texture2D frame)
    {
        var currentDesc = frame.Description;
        const int bitsPerPixel = 32;
        return (currentDesc.Width * currentDesc.Height * bitsPerPixel + 7) / 8;
    }

    // FIXME: Would possibly need some refactoring not to duplicate code from ScreenshotWriter
    private static float Read2BytesToHalf(DataStream imageStream)
    {
        var low = (byte)imageStream.ReadByte();
        var high = (byte)imageStream.ReadByte();
        return  FormatConversion.ToTwoByteFloat(low, high);
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
        var audioSample = _audioWriter.CreateSampleFromFrame(audioFrame);
        try
        {
            var samples = new Dictionary<int, Sample>();
            samples.Add(StreamIndex, videoSample);
            samples.Add(_audioWriter.StreamIndex, audioSample);

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

        MediaFactory.FrameRateToAverageTimePerFrame(Framerate, 1, out var frameDuration);

        foreach (var item in samples)
        {
            var streamIndex = item.Key;
            var sample = item.Value;

            sample.SampleTime = frameDuration * _frameIndex;
            sample.SampleDuration = frameDuration;

            SinkWriter.WriteSample(streamIndex, sample);
        }
    }

    /// <summary>
    /// Creates a media target.
    /// </summary>
    /// <param name="sinkWriter">The previously created SinkWriter.</param>
    /// <param name="videoPixelSize">The pixel size of the video.</param>
    /// <param name="streamIndex">The stream index for the new target.</param>
    protected abstract void CreateMediaTarget(MF.SinkWriter sinkWriter, Int2 videoPixelSize, out int streamIndex);

    /// <summary>
    /// Internal use: FlipY during rendering?
    /// </summary>
    protected virtual bool FlipY => false;

    public int Bitrate { get; set; }

    public int Framerate { get; set; }

    private static void DisposeTextures()
    {
        foreach (var image in _imagesWithCpuAccess)
            image.Dispose();

        _imagesWithCpuAccess.Clear();
    }

    #region IDisposable Support
    public void Dispose()
    {
        if (SinkWriter != null)
        {
            // since we will try to write on shutdown, things can still go wrong
            try
            {
                SinkWriter.NotifyEndOfSegment(_streamIndex);
                if (_frameIndex > 0)
                {
                    SinkWriter.Finalize();
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }
            finally
            {
                SinkWriter.Dispose();
                SinkWriter = null;
            }
        }

        // dispose textures too
        DisposeTextures();
    }
    #endregion

    #region Resources for MediaFoundation video rendering
    // private MF.ByteStream outStream;
    private readonly Int2 _videoPixelSize;
    private int _frameIndex;
    private int _streamIndex;

    // Hold several textures internally to speed up calculations
    private const int NumTextureEntries = 3;
    private static readonly List<Texture2D> _imagesWithCpuAccess = new();
    private static int _currentIndex;
    private static int _currentUsageIndex;
    #endregion

    private int StreamIndex => _streamIndex;

    private MF.SinkWriter SinkWriter { get; set; }
    private MediaFoundationAudioWriter _audioWriter;

    private static readonly Guid _videoInputFormatId = MF.VideoFormatGuids.Rgb32;
    private static readonly bool _mfInitialized = false;

    private readonly bool _supportAudio;
    private readonly Guid _videoInputFormat;
}

internal class Mp4VideoWriter : MfVideoWriter
{
    private static readonly Guid _h264EncodingFormatId = MF.VideoFormatGuids.H264;

    public Mp4VideoWriter(string filePath, Int2 videoPixelSize)
        : base(filePath, videoPixelSize)
    {
    }

    protected override void CreateMediaTarget(SinkWriter sinkWriter, Int2 videoPixelSize, out int streamIndex)
    {
        using var mediaTypeOut = new MF.MediaType();
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.Subtype, _h264EncodingFormatId);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.AvgBitrate, Bitrate);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.InterlaceMode, (int)MF.VideoInterlaceMode.Progressive);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.FrameSize, MfHelper.GetMfEncodedIntsByValues(videoPixelSize.Width, videoPixelSize.Height));
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.FrameRate, MfHelper.GetMfEncodedIntsByValues(Framerate, 1));
        sinkWriter.AddStream(mediaTypeOut, out streamIndex);
    }

    /// <summary>
    /// Internal use: FlipY during rendering?
    /// </summary>
    protected override bool FlipY => true;
}

