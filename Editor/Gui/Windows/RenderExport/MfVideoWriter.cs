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
using T3.Core.Logging;
using T3.Core.Utils;
using MF = SharpDX.MediaFoundation;

namespace T3.Editor.Gui.Windows.RenderExport;

internal abstract class MfVideoWriter : IDisposable
{
    public static List<Format> SupportedFormats { get; } = new()
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

    private MF.Sample CreateSampleFromFrame(ref Texture2D originalTexture)
    {
        if (originalTexture == null)
            return null;

        // Write all contents to the MediaBuffer for media foundation
        var mediaBuffer = MF.MediaFactory.CreateMemoryBuffer(RgbaSizeInBytes(ref originalTexture));

        //var device = ResourceManager.Device;
        DataStream outputStream = null;
        try
        {
            if (TextureReadAccess.Setup(originalTexture, out var dataBox, out var inputStream))
            {
                // Create an 8 bit RGBA output buffer to write to
                var width = originalTexture.Description.Width;
                var height = originalTexture.Description.Height;
                var formatId = PixelFormat.Format32bppRGBA;
                var rowStride = PixelFormat.GetStride(formatId, width);

                //var pixelByteCount = PixelFormat.GetStride(formatId, 1);
                var outBufferSize = height * rowStride;
                outputStream = new DataStream(outBufferSize, true, true);

                var mediaBufferPointer = mediaBuffer.Lock(out _, out _);

                // Note: dataBox.RowPitch and outputStream.RowPitch can diverge if width is not divisible by 16.
                for (int loopY = 0; loopY < _videoPixelSize.Height; loopY++)
                {
                    if (!FlipY)
                        inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                    else
                        inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

                    outputStream.WriteRange(inputStream.ReadRange<byte>(rowStride));
                }

                // Copy our finished BGRA buffer to the media buffer pointer
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
                inputStream?.Dispose();

                // TextureReadAccess.Release();
            }
        }
        catch (Exception e)
        {
            Log.Warning("Internal image copy failed : " + e);
            //throw new InvalidOperationException("Internal image copy failed : " + e);
        }
        finally
        {
            outputStream?.Dispose();
            mediaBuffer.Unlock();
            mediaBuffer.CurrentLength = RgbaSizeInBytes(ref TextureReadAccess.ConversionTexture);
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
        return FormatConversion.ToTwoByteFloat(low, high);
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

        TextureReadAccess.DisposeTextures();
    }
    #endregion

    #region Resources for MediaFoundation video rendering
    // private MF.ByteStream outStream;
    private readonly Int2 _videoPixelSize;
    private int _frameIndex;
    private int _streamIndex;
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