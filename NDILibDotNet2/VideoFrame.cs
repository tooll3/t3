using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace NewTek.NDI
{
    public class VideoFrame : IDisposable
    {
        // the simple constructor only deals with BGRA. For other color formats you'll need to handle it manually.
        // Defaults to progressive but can be changed.
        public VideoFrame(int width, int height, float aspectRatio, int frameRateNumerator, int frameRateDenominator,
                            NDIlib.frame_format_type_e format = NDIlib.frame_format_type_e.frame_format_type_progressive)
        {
            // we have to know to free it later
            _memoryOwned = true;

            int stride = (width * 32 /*BGRA bpp*/ + 7) / 8;
            int bufferSize = height * stride;

            // allocate some memory for a video buffer
            IntPtr videoBufferPtr = Marshal.AllocHGlobal(bufferSize);

            _ndiVideoFrame = new NDIlib.video_frame_v2_t()
            {
                xres = width,
                yres = height,
                FourCC = NDIlib.FourCC_type_e.FourCC_type_BGRA,
                frame_rate_N = frameRateNumerator,
                frame_rate_D = frameRateDenominator,
                picture_aspect_ratio = aspectRatio,
                frame_format_type = format,
                timecode = NDIlib.send_timecode_synthesize,
                p_data = videoBufferPtr,
                line_stride_in_bytes = stride,
                p_metadata = IntPtr.Zero,
                timestamp = 0
            };
        }

        public VideoFrame(IntPtr bufferPtr, int width, int height, int stride, NDIlib.FourCC_type_e fourCC,
                            float aspectRatio, int frameRateNumerator, int frameRateDenominator, NDIlib.frame_format_type_e format)
        {
            _ndiVideoFrame = new NDIlib.video_frame_v2_t()
            {
                xres = width,
                yres = height,
                FourCC = fourCC,
                frame_rate_N = frameRateNumerator,
                frame_rate_D = frameRateDenominator,
                picture_aspect_ratio = aspectRatio,
                frame_format_type = format,
                timecode = NDIlib.send_timecode_synthesize,
                p_data = bufferPtr,
                line_stride_in_bytes = stride,
                p_metadata = IntPtr.Zero,
                timestamp = 0
            };
        }

        public int Width
        {
            get
            {
                return _ndiVideoFrame.xres;
            }
        }

        public int Height
        {
            get
            {
                return _ndiVideoFrame.yres;
            }
        }

        public int Stride
        {
            get
            {
                return _ndiVideoFrame.line_stride_in_bytes;
            }
        }

        public IntPtr BufferPtr
        {
            get
            {
                return _ndiVideoFrame.p_data;
            }
        }

        public Int64 TimeStamp
        {
            get
            {
                return _ndiVideoFrame.timestamp;
            }
            set
            {
                _ndiVideoFrame.timestamp = value;
            }
        }

        public XElement MetaData
        {
            get
            {
                if (_ndiVideoFrame.p_metadata == IntPtr.Zero)
                    return null;

                String mdString = UTF.Utf8ToString(_ndiVideoFrame.p_metadata);
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

        ~VideoFrame()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_memoryOwned)
                {
                    Marshal.FreeHGlobal(_ndiVideoFrame.p_data);
                    _ndiVideoFrame.p_data = IntPtr.Zero;
                }
            }
        }

        internal NDIlib.video_frame_v2_t _ndiVideoFrame;
        bool _memoryOwned = false;
    }
}
