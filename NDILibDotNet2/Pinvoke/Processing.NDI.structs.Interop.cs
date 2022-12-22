// NOTE : The following MIT license applies to this file ONLY and not to the SDK as a whole. Please review the SDK documentation 
// for the description of the full license terms, which are also provided in the file "NDI License Agreement.pdf" within the SDK or 
// online at http://new.tk/ndisdk_license/. Your use of any part of this SDK is acknowledgment that you agree to the SDK license 
// terms. The full NDI SDK may be downloaded at http://ndi.tv/
//
//*************************************************************************************************************************************
// 
// Copyright (C)2014-2021, NewTek, inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
// files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace NewTek
{
	[SuppressUnmanagedCodeSecurity]
	public static partial class NDIlib
	{
		// An enumeration to specify the type of a packet returned by the functions
		public enum frame_type_e
		{
			frame_type_none = 0,
			frame_type_video = 1,
			frame_type_audio = 2,
			frame_type_metadata = 3,
			frame_type_error = 4,

			// This indicates that the settings on this input have changed.
			// For instamce, this value will be returned from NDIlib_recv_capture_v2 and NDIlib_recv_capture
			// when the device is known to have new settings, for instance the web-url has changed ot the device
			// is now known to be a PTZ camera.
			frame_type_status_change = 100
		}

		public enum FourCC_type_e
		{
			// YCbCr color space
			FourCC_type_UYVY = 0x59565955,

            // 4:2:0 formats
            NDIlib_FourCC_video_type_YV12 = 0x32315659,
            NDIlib_FourCC_video_type_NV12 = 0x3231564E,
            NDIlib_FourCC_video_type_I420 = 0x30323449,

			// BGRA
			FourCC_type_BGRA = 0x41524742,
			FourCC_type_BGRX = 0x58524742,

			// RGBA
			FourCC_type_RGBA = 0x41424752,
			FourCC_type_RGBX = 0x58424752,

			// This is a UYVY buffer followed immediately by an alpha channel buffer.
			// If the stride of the YCbCr component is "stride", then the alpha channel
			// starts at image_ptr + yres*stride. The alpha channel stride is stride/2.
			FourCC_type_UYVA = 0x41565955
		}

		public enum frame_format_type_e
		{
			// A progressive frame
			frame_format_type_progressive = 1,

			// A fielded frame with the field 0 being on the even lines and field 1 being
			// on the odd lines/
			frame_format_type_interleaved = 0,

			// Individual fields
			frame_format_type_field_0 = 2,
			frame_format_type_field_1 = 3
		}

        // FourCC values for audio frames
        public enum FourCC_audio_type_e
        {
            // Planar 32-bit floating point. Be sure to specify the channel stride.
            FourCC_audio_type_FLTP = 0x70544c46,
            FourCC_type_FLTP = FourCC_audio_type_FLTP,

            // Ensure that the size is 32bits
            FourCC_audio_type_max = 0x7fffffff
        }

        // This is a descriptor of a NDI source available on the network.
        [StructLayoutAttribute(LayoutKind.Sequential)]
		public struct source_t
		{
			// A UTF8 string that provides a user readable name for this source.
			// This can be used for serialization, etc... and comprises the machine
			// name and the source name on that machine. In the form
			//		MACHINE_NAME (NDI_SOURCE_NAME)
			// If you specify this parameter either as NULL, or an EMPTY string then the
			// specific ip addres adn port number from below is used.
			public IntPtr	p_ndi_name;

            // A UTF8 string that provides the actual network address and any parameters. 
            // This is not meant to be application readable and might well change in the future.
            // This can be nullptr if you do not know it and the API internally will instantiate
            // a finder that is used to discover it even if it is not yet available on the network.
            public IntPtr p_url_address;
		}

		// This describes a video frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct video_frame_v2_t
		{
			// The resolution of this frame
			public int xres,	yres;

			// What FourCC this is with. This can be two values
			public FourCC_type_e	FourCC;

			// What is the frame-rate of this frame.
			// For instance NTSC is 30000,1001 = 30000/1001 = 29.97fps
			public int frame_rate_N,	frame_rate_D;

			// What is the picture aspect ratio of this frame.
			// For instance 16.0/9.0 = 1.778 is 16:9 video
			// 0 means square pixels
			public float	picture_aspect_ratio;

			// Is this a fielded frame, or is it progressive
			public frame_format_type_e	frame_format_type;

			// The timecode of this frame in 100ns intervals
			public Int64	timecode;

			// The video data itself
			public IntPtr	p_data;

			// The inter line stride of the video data, in bytes.
			public int	line_stride_in_bytes;

			// Per frame metadata for this frame. This is a NULL terminated UTF8 string that should be
			// in XML format. If you do not want any metadata then you may specify NULL here.
			public IntPtr	p_metadata;

			// This is only valid when receiving a frame and is specified as a 100ns time that was the exact
			// moment that the frame was submitted by the sending side and is generated by the SDK. If this
			// value is NDIlib_recv_timestamp_undefined then this value is not available and is NDIlib_recv_timestamp_undefined.
			public Int64	timestamp;
		}

		// This describes an audio frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct audio_frame_v2_t
		{
			// The sample-rate of this buffer
			public int	sample_rate;

			// The number of audio channels
			public int	no_channels;

			// The number of audio samples per channel
			public int	no_samples;

			// The timecode of this frame in 100ns intervals
			public Int64	timecode;

			// The audio data
			public IntPtr	p_data;

			// The inter channel stride of the audio channels, in bytes
			public int	channel_stride_in_bytes;

			// Per frame metadata for this frame. This is a NULL terminated UTF8 string that should be
			// in XML format. If you do not want any metadata then you may specify NULL here.
			public IntPtr	p_metadata;

			// This is only valid when receiving a frame and is specified as a 100ns time that was the exact
			// moment that the frame was submitted by the sending side and is generated by the SDK. If this
			// value is NDIlib_recv_timestamp_undefined then this value is not available and is NDIlib_recv_timestamp_undefined.
			public Int64	timestamp;
		}

        // This describes an audio frame
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct audio_frame_v3_t
        {
            // The sample-rate of this buffer
            public int sample_rate;

            // The number of audio channels
            public int no_channels;

            // The number of audio samples per channel
            public int no_samples;

            // The timecode of this frame in 100ns intervals
            public Int64 timecode;

            // What FourCC describing the type of data for this frame
            FourCC_audio_type_e FourCC;

            // The audio data
            public IntPtr p_data;

            // If the FourCC is not a compressed type and the audio format is planar,
            // then this will be the stride in bytes for a single channel.
            // If the FourCC is a compressed type, then this will be the size of the
            // p_data buffer in bytes.
            public int channel_stride_in_bytes;

            // Per frame metadata for this frame. This is a NULL terminated UTF8 string that should be
            // in XML format. If you do not want any metadata then you may specify NULL here.
            public IntPtr p_metadata;

            // This is only valid when receiving a frame and is specified as a 100ns time that was the exact
            // moment that the frame was submitted by the sending side and is generated by the SDK. If this
            // value is NDIlib_recv_timestamp_undefined then this value is not available and is NDIlib_recv_timestamp_undefined.
            public Int64 timestamp;
        }

        // The data description for metadata
        [StructLayoutAttribute(LayoutKind.Sequential)]
		public struct metadata_frame_t
		{
			// The length of the string in UTF8 characters. This includes the NULL terminating character.
			// If this is 0, then the length is assume to be the length of a NULL terminated string.
			public int	length;

			// The timecode of this frame in 100ns intervals
			public Int64	timecode;

			// The metadata as a UTF8 XML string. This is a NULL terminated string.
			public IntPtr	p_data;
		}

		// Tally structures
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct tally_t
		{
			// Is this currently on program output
			[MarshalAsAttribute(UnmanagedType.U1)]
			public bool	on_program;

			// Is this currently on preview output
			[MarshalAsAttribute(UnmanagedType.U1)]
			public bool	on_preview;
		}

		// When you specify this as a timecode, the timecode will be synthesized for you. This may
		// be used when sending video, audio or metadata. If you never specify a timecode at all,
		// asking for each to be synthesized, then this will use the current system time as the
		// starting timecode and then generate synthetic ones, keeping your streams exactly in
		// sync as long as the frames you are sending do not deviate from the system time in any
		// meaningful way. In practice this means that if you never specify timecodes that they
		// will always be generated for you correctly. Timecodes coming from different senders on
		// the same machine will always be in sync with eachother when working in this way. If you
		// have NTP installed on your local network, then streams can be synchronized between
		// multiple machines with very high precision.
		//
		// If you specify a timecode at a particular frame (audio or video), then ask for all subsequent
		// ones to be synthesized. The subsequent ones will be generated to continue this sequency
		// maintining the correct relationship both the between streams and samples generated, avoiding
		// them deviating in time from the timecode that you specified in any meanginfful way.
		//
		// If you specify timecodes on one stream (e.g. video) and ask for the other stream (audio) to
		// be sythesized, the correct timecodes will be generated for the other stream and will be synthesize
		// exactly to match (they are not quantized inter-streams) the correct sample positions.
		//
		// When you send metadata messagesa and ask for the timecode to be synthesized, then it is chosen
		// to match the closest audio or video frame timecode so that it looks close to something you might
		// want ... unless there is no sample that looks close in which a timecode is synthesized from the
		// last ones known and the time since it was sent.
		//
		public static Int64 send_timecode_synthesize =  Int64.MaxValue;
        
		// If the time-stamp is not available (i.e. a version of a sender before v2.5)
		public static Int64 recv_timestamp_undefined =  Int64.MaxValue;

	} // class NDIlib

} // namespace NewTek

