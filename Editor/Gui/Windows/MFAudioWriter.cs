/*

Based on the MIT license video writing code at
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


namespace Editor.Gui.Windows
{
    struct WAVEFORMATEX
    {
        public SharpDX.Multimedia.WaveFormatEncoding wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;

        public static WAVEFORMATEX DefaultPCM
        {
            get
            {
                var WaveFormatEx = new WAVEFORMATEX();
                WaveFormatEx.wFormatTag = SharpDX.Multimedia.WaveFormatEncoding.Pcm;
                WaveFormatEx.nChannels = 1;
                WaveFormatEx.nSamplesPerSec = 16000;
                WaveFormatEx.wBitsPerSample = 32;
                WaveFormatEx.nBlockAlign = (ushort)(WaveFormatEx.nChannels * WaveFormatEx.wBitsPerSample / 8);
                WaveFormatEx.nAvgBytesPerSec = WaveFormatEx.nSamplesPerSec * WaveFormatEx.nBlockAlign;
                WaveFormatEx.cbSize = 0;

                return WaveFormatEx;
            }
        }

        public static WAVEFORMATEX DefaultIEEE
        {
            get
            {
                var WaveFormatEx = new WAVEFORMATEX();
                WaveFormatEx.wFormatTag = SharpDX.Multimedia.WaveFormatEncoding.IeeeFloat;
                WaveFormatEx.nChannels = 1;
                WaveFormatEx.nSamplesPerSec = 16000;
                WaveFormatEx.wBitsPerSample = 32;
                WaveFormatEx.nBlockAlign = (ushort)(WaveFormatEx.nChannels * WaveFormatEx.wBitsPerSample / 8);
                WaveFormatEx.nAvgBytesPerSec = WaveFormatEx.nSamplesPerSec * WaveFormatEx.nBlockAlign;
                WaveFormatEx.cbSize = 0;

                return WaveFormatEx;
            }
        }

        public SharpDX.Multimedia.WaveFormat ToSharpDX()
        {
            return SharpDX.Multimedia.WaveFormat.CreateCustomFormat(
                wFormatTag,
                (int)nSamplesPerSec,
                nChannels,
                (int)nAvgBytesPerSec,
                nBlockAlign,
                wBitsPerSample
            );
        }
    }

    class MediaFoundationAudioWriter
    {
        /// <summary>
        /// Gets all the available media types for a particular 
        /// </summary>
        /// <param name="audioSubtype">Audio subtype - a value from the AudioSubtypes class</param>
        /// <returns>An array of available media types that can be encoded with this subtype</returns>
        public static MF.MediaType[] GetOutputMediaTypes(Guid audioSubtype)
        {
            MF.Collection availableTypes;
            try
            {
                availableTypes = MF.MediaFactory.TranscodeGetAudioOutputAvailableTypes(audioSubtype, MF.TransformEnumFlag.All, null);
            }
            catch (SharpDXException c)
            {
                if (c.ResultCode.Code == MF.ResultCode.NotFound.Code)
                {
                    // Don't worry if we didn't find any - just means no encoder available for this type
                    return new MF.MediaType[0];
                }
                throw;
            }

            int count = availableTypes.ElementCount;
            var mediaTypes = new List<MF.MediaType>(count);
            for (int n = 0; n < count; n++)
            {
                var mediaTypeObject = availableTypes.GetElement(n);
                mediaTypes.Add(new MF.MediaType((System.IntPtr)(mediaTypeObject.AddReference())));
                mediaTypeObject.Release();
            }
            availableTypes.Dispose();
            return mediaTypes.ToArray();
        }

        /// <summary>
        /// Queries the available bitrates for a given encoding output type, sample rate and number of channels
        /// </summary>
        /// <param name="audioSubtype">Audio subtype - a value from the AudioSubtypes class</param>
        /// <param name="sampleRate">The sample rate of the PCM to encode</param>
        /// <param name="channels">The number of channels of the PCM to encode</param>
        /// <returns>An array of available bitrates in average bits per second</returns>
        public static int[] GetEncodeBitrates(Guid audioSubtype, int sampleRate, int channels)
        {
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.Get(MF.MediaTypeAttributeKeys.AudioSamplesPerSecond) == sampleRate &&
                    mt.Get(MF.MediaTypeAttributeKeys.AudioNumChannels) == channels)
                .Select(mt => mt.Get(MF.MediaTypeAttributeKeys.AudioAvgBytesPerSecond) * 8)
                .Distinct()
                .OrderBy(br => br)
                .ToArray();
        }

        /// <summary>
        /// Tries to find the encoding media type with the closest bitrate to that specified
        /// </summary>
        /// <param name="audioSubtype">Audio subtype, a value from AudioSubtypes</param>
        /// <param name="inputFormat">Your encoder input format (used to check sample rate and channel count)</param>
        /// <param name="desiredBitRate">Your desired bitrate</param>
        /// <returns>The closest media type, or null if none available</returns>
        public static MF.MediaType SelectMediaType(Guid audioSubtype, SharpDX.Multimedia.WaveFormat inputFormat, int desiredBitRate)
        {
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.Get(MF.MediaTypeAttributeKeys.AudioSamplesPerSecond) == inputFormat.SampleRate &&
                    mt.Get(MF.MediaTypeAttributeKeys.AudioNumChannels) == inputFormat.Channels)
                .Select(mt => new { MediaType = mt, Delta = Math.Abs(desiredBitRate - mt.Get(MF.MediaTypeAttributeKeys.AudioAvgBytesPerSecond) * 8) })
                .OrderBy(mt => mt.Delta)
                .Select(mt => mt.MediaType)
                .FirstOrDefault();
        }

        private int streamIndex;
        public int StreamIndex => streamIndex;

        public virtual Guid AudioFormat { get; }

        public MediaFoundationAudioWriter(MF.SinkWriter sinkWriter, ref WAVEFORMATEX waveFormat, int desiredBitRate = 192000)
        {
            var sharpWf = waveFormat.ToSharpDX();

            // Information on configuring an AAC media type can be found here:
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd742785%28v=vs.85%29.aspx
            var outputMediaType = SelectMediaType(AudioFormat, sharpWf, desiredBitRate);
            if (outputMediaType == null) throw new InvalidOperationException("No suitable encoders available");

            var inputMediaType = new MF.MediaType();
            var size = 18 + sharpWf.ExtraSize;

            sinkWriter.AddStream(outputMediaType, out streamIndex);

            MF.MediaFactory.InitMediaTypeFromWaveFormatEx(inputMediaType, new[] { sharpWf }, size);
            sinkWriter.SetInputMediaType(streamIndex, inputMediaType, null);
        }

        public MF.Sample CreateSampleFromFrame(byte[] data)
        {
            MF.MediaBuffer mediaBuffer = MF.MediaFactory.CreateMemoryBuffer(data.Length);

            // Write all contents to the MediaBuffer for media foundation
            int cbMaxLength = 0;
            int cbCurrentLength = 0;
            IntPtr mediaBufferPointer = mediaBuffer.Lock(out cbMaxLength, out cbCurrentLength);
            try
            {

                Marshal.Copy(data, 0, mediaBufferPointer, data.Length);
            }
            finally
            {
                mediaBuffer.Unlock();
                mediaBuffer.CurrentLength = data.Length;
            }

            // Create the sample (includes image and timing information)
            MF.Sample sample = MF.MediaFactory.CreateSample();
            sample.AddBuffer(mediaBuffer);

            return sample;
        }
    }

    class MP3AudioWriter : MediaFoundationAudioWriter
    {
        public MP3AudioWriter(MF.SinkWriter sinkWriter, ref WAVEFORMATEX waveFormat, int desiredBitRate = 192000)
            : base(sinkWriter, ref waveFormat, desiredBitRate)
        {
        }

        public override Guid AudioFormat
        {
            get
            {
                return MF.AudioFormatGuids.Mp3;
            }
        }
    }

} // namespace
