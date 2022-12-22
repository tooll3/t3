using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace NewTek.NDI
{
    public class AudioFrame : IDisposable
    {
        public AudioFrame(int maxSamples, int sampleRate, int numChannels)
        {
            // we have to know to free it later
            _memoryOwned = true;

            IntPtr audioBufferPtr = Marshal.AllocHGlobal(numChannels * maxSamples * sizeof(float));

            _ndiAudioFrame = new NDIlib.audio_frame_v2_t()
            {
                sample_rate = sampleRate,
                no_channels = numChannels,
                no_samples = maxSamples,
                timecode = NDIlib.send_timecode_synthesize,
                p_data = audioBufferPtr,
                channel_stride_in_bytes = sizeof(float) * maxSamples,
                p_metadata = IntPtr.Zero,
                timestamp = 0
            };
        }

        public AudioFrame(IntPtr bufferPtr, int sampleRate, int numChannels, int channelStride, int numSamples)
        {
            _ndiAudioFrame = new NDIlib.audio_frame_v2_t()
            {
                sample_rate = 48000,
                no_channels = 2,
                no_samples = 1602,
                timecode = NDIlib.send_timecode_synthesize,
                p_data = bufferPtr,
                channel_stride_in_bytes = channelStride,
                p_metadata = IntPtr.Zero,
                timestamp = 0
            };
        }

        public IntPtr AudioBuffer
        {
            get
            {
                return _ndiAudioFrame.p_data;
            }
        }

        public int NumSamples
        {
            get
            {
                return _ndiAudioFrame.no_samples;
            }

            set
            {
                _ndiAudioFrame.no_samples = value;
            }
        }

        public int NumChannels
        {
            get
            {
                return _ndiAudioFrame.no_channels;
            }

            set
            {
                _ndiAudioFrame.no_channels = value;
            }
        }

        public int ChannelStride
        {
            get
            {
                return _ndiAudioFrame.channel_stride_in_bytes;
            }

            set
            {
                _ndiAudioFrame.channel_stride_in_bytes = value;
            }
        }

        public int SampleRate
        {
            get
            {
                return _ndiAudioFrame.sample_rate;
            }

            set
            {
                _ndiAudioFrame.sample_rate = value;
            }
        }

        public Int64 TimeStamp
        {
            get
            {
                return _ndiAudioFrame.timestamp;
            }
            set
            {
                _ndiAudioFrame.timestamp = value;
            }
        }

        public XElement MetaData
        {
            get
            {
                if (_ndiAudioFrame.p_metadata == IntPtr.Zero)
                    return null;

                String mdString = UTF.Utf8ToString(_ndiAudioFrame.p_metadata);
                if (String.IsNullOrEmpty(mdString))
                    return null;

                return XElement.Parse(mdString);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AudioFrame()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_memoryOwned && _ndiAudioFrame.p_data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_ndiAudioFrame.p_data);
                    _ndiAudioFrame.p_data = IntPtr.Zero;
                }
            }
        }

        internal NDIlib.audio_frame_v2_t _ndiAudioFrame;
        bool _memoryOwned = false;
    }
}
