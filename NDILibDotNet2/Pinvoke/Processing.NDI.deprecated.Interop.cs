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
		// This describes a video frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct video_frame_t
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
			public float	picture_aspect_ratio;

			// Is this a fielded frame, or is it progressive
			public frame_format_type_e	frame_format_type;

			// The timecode of this frame in 100ns intervals
			public Int64	timecode;

			// The video data itself
			public IntPtr	p_data;

			// The inter line stride of the video data, in bytes.
			public int	line_stride_in_bytes;
		}

		// This describes an audio frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct audio_frame_t
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
		}

		// The creation structure that is used when you are creating a receiver
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct recv_create_t
		{
			// The source that you wish to connect to.
			public source_t	source_to_connect_to;

			// Your preference of color space. See above.
			public recv_color_format_e	color_format;

			// The bandwidth setting that you wish to use for this video source. Bandwidth
			// controlled by changing both the compression level and the resolution of the source.
			// A good use for low bandwidth is working on WIFI connections.
			public recv_bandwidth_e	bandwidth;

			// When this flag is FALSE, all video that you receive will be progressive. For sources
			// that provide fields, this is de-interlaced on the receiving side (because we cannot change
			// what the up-stream source was actually rendering. This is provided as a convenience to
			// down-stream sources that do not wish to understand fielded video. There is almost no
			// performance impact of using this function.
			[MarshalAsAttribute(UnmanagedType.U1)]
			public bool	allow_video_fields;
		}

		// For legacy reasons I called this the wrong thing. For backwards compatibility.
		[Obsolete("find_create2 is obsolete.", false)]
		public static IntPtr find_create2(ref find_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.find_create2_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.find_create2_32(ref p_create_settings);
		}

		[Obsolete("find_create is obsolete.", false)]
		public static IntPtr find_create(ref find_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.find_create_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.find_create_32(ref p_create_settings);
		}

		// DEPRECATED. This function is basically exactly the following and was confusing to use.
		//		if ((!timeout_in_ms) || (NDIlib_find_wait_for_sources(timeout_in_ms)))
		//				return NDIlib_find_get_current_sources(p_instance, p_no_sources);
		//		return NULL;
		[Obsolete("find_get_sources is obsolete.", false)]
		public static IntPtr find_get_sources(IntPtr p_instance, ref UInt32 p_no_sources, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.find_get_sources_64( p_instance, ref p_no_sources,  timeout_in_ms);
			else
				return  UnsafeNativeMethods.find_get_sources_32( p_instance, ref p_no_sources,  timeout_in_ms);
		}

		// This function is deprecated, please use NDIlib_recv_create_v3 if you can. Using this function will continue to work, and be
		// supported for backwards compatibility. This version sets the receiver name to NULL.
		[Obsolete("recv_create_v2 is obsolete.", false)]
		public static IntPtr recv_create_v2(ref recv_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_create_v2_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.recv_create_v2_32(ref p_create_settings);
		}

		// For legacy reasons I called this the wrong thing. For backwards compatibility.
		[Obsolete("recv_create2 is obsolete.", false)]
		public static IntPtr recv_create2(ref recv_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_create2_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.recv_create2_32(ref p_create_settings);
		}

		// This function is deprecated, please use NDIlib_recv_create_v3 if you can. Using this function will continue to work, and be
		// supported for backwards compatibility. This version sets bandwidth to highest and allow fields to true.
		[Obsolete("recv_create is obsolete.", false)]
		public static IntPtr recv_create(ref recv_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_create_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.recv_create_32(ref p_create_settings);
		}

		// This will allow you to receive video, audio and metadata frames.
		// Any of the buffers can be NULL, in which case data of that type
		// will not be captured in this call. This call can be called simultaneously
		// on separate threads, so it is entirely possible to receive audio, video, metadata
		// all on separate threads. This function will return NDIlib_frame_type_none if no
		// data is received within the specified timeout and NDIlib_frame_type_error if the connection is lost.
		// Buffers captured with this must be freed with the appropriate free function below.
		[Obsolete("recv_capture is obsolete.", false)]
		public static frame_type_e recv_capture(IntPtr p_instance, ref video_frame_t p_video_data, ref audio_frame_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_capture_64( p_instance, ref p_video_data, ref p_audio_data, ref p_metadata,  timeout_in_ms);
			else
				return  UnsafeNativeMethods.recv_capture_32( p_instance, ref p_video_data, ref p_audio_data, ref p_metadata,  timeout_in_ms);
		}

		// Free the buffers returned by capture for video
		[Obsolete("recv_free_video is obsolete.", false)]
		public static void recv_free_video(IntPtr p_instance, ref video_frame_t p_video_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_free_video_64( p_instance, ref p_video_data);
			else
				 UnsafeNativeMethods.recv_free_video_32( p_instance, ref p_video_data);
		}

		// Free the buffers returned by capture for audio
		[Obsolete("recv_free_audio is obsolete.", false)]
		public static void recv_free_audio(IntPtr p_instance, ref audio_frame_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_free_audio_64( p_instance, ref p_audio_data);
			else
				 UnsafeNativeMethods.recv_free_audio_32( p_instance, ref p_audio_data);
		}

		// This will add a video frame
		[Obsolete("send_send_video is obsolete.", false)]
		public static void send_send_video(IntPtr p_instance, ref video_frame_t p_video_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_video_64( p_instance, ref p_video_data);
			else
				 UnsafeNativeMethods.send_send_video_32( p_instance, ref p_video_data);
		}

		// This will add a video frame and will return immediately, having scheduled the frame to be displayed.
		// All processing and sending of the video will occur asynchronously. The memory accessed by NDIlib_video_frame_t
		// cannot be freed or re-used by the caller until a synchronizing event has occurred. In general the API is better
		// able to take advantage of asynchronous processing than you might be able to by simple having a separate thread
		// to submit frames.
		//
		// This call is particularly beneficial when processing BGRA video since it allows any color conversion, compression
		// and network sending to all be done on separate threads from your main rendering thread.
		//
		// Synchronizing events are :
		//		- a call to NDIlib_send_send_video
		//		- a call to NDIlib_send_send_video_async with another frame to be sent
		//		- a call to NDIlib_send_send_video with p_video_data=NULL
		//		- a call to NDIlib_send_destroy
		[Obsolete("send_send_video_async is obsolete.", false)]
		public static void send_send_video_async(IntPtr p_instance, ref video_frame_t p_video_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_video_async_64( p_instance, ref p_video_data);
			else
				 UnsafeNativeMethods.send_send_video_async_32( p_instance, ref p_video_data);
		}

		// This will add an audio frame
		[Obsolete("send_send_audio is obsolete.", false)]
		public static void send_send_audio(IntPtr p_instance, ref audio_frame_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_audio_64( p_instance, ref p_audio_data);
			else
				 UnsafeNativeMethods.send_send_audio_32( p_instance, ref p_audio_data);
		}

		// Convert an planar floating point audio buffer into a interleaved short audio buffer.
		// IMPORTANT : You must allocate the space for the samples in the destination to allow for your own memory management.
		[Obsolete("util_audio_to_interleaved_16s is obsolete.", false)]
		public static void util_audio_to_interleaved_16s(ref audio_frame_t p_src, ref audio_frame_interleaved_16s_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_to_interleaved_16s_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_to_interleaved_16s_32(ref p_src, ref p_dst);
		}

		// Convert an interleaved short audio buffer audio buffer into a planar floating point one.
		// IMPORTANT : You must allocate the space for the samples in the destination to allow for your own memory management.
		[Obsolete("util_audio_from_interleaved_16s is obsolete.", false)]
		public static void util_audio_from_interleaved_16s(ref audio_frame_interleaved_16s_t p_src, ref audio_frame_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_from_interleaved_16s_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_from_interleaved_16s_32(ref p_src, ref p_dst);
		}

		// Convert an planar floating point audio buffer into a interleaved floating point audio buffer.
		// IMPORTANT : You must allocate the space for the samples in the destination to allow for your own memory management.
		[Obsolete("util_audio_to_interleaved_32f is obsolete.", false)]
		public static void util_audio_to_interleaved_32f(ref audio_frame_t p_src, ref audio_frame_interleaved_32f_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_to_interleaved_32f_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_to_interleaved_32f_32(ref p_src, ref p_dst);
		}

		// Convert an interleaved floating point audio buffer into a planar floating point one.
		// IMPORTANT : You must allocate the space for the samples in the destination to allow for your own memory management.
		[Obsolete("util_audio_from_interleaved_32f is obsolete.", false)]
		public static void util_audio_from_interleaved_32f(ref audio_frame_interleaved_32f_t p_src, ref audio_frame_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_from_interleaved_32f_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_from_interleaved_32f_32(ref p_src, ref p_dst);
		}

		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// find_create2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr find_create2_64(ref find_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_find_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr find_create2_32(ref find_create_t p_create_settings);

			// find_create 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr find_create_64(ref find_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_find_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr find_create_32(ref find_create_t p_create_settings);

			// find_get_sources 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_get_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr find_get_sources_64(IntPtr p_instance, ref UInt32 p_no_sources, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_find_get_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr find_get_sources_32(IntPtr p_instance, ref UInt32 p_no_sources, UInt32 timeout_in_ms);

			// recv_create_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_create_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create_v2_64(ref recv_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_create_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create_v2_32(ref recv_create_t p_create_settings);

			// recv_create2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create2_64(ref recv_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create2_32(ref recv_create_t p_create_settings);

			// recv_create 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create_64(ref recv_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create_32(ref recv_create_t p_create_settings);

			// recv_capture 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern frame_type_e recv_capture_64(IntPtr p_instance, ref video_frame_t p_video_data, ref audio_frame_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern frame_type_e recv_capture_32(IntPtr p_instance, ref video_frame_t p_video_data, ref audio_frame_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);

			// recv_free_video 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_video", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_video_64(IntPtr p_instance, ref video_frame_t p_video_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_video", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_video_32(IntPtr p_instance, ref video_frame_t p_video_data);

			// recv_free_audio 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_audio", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_audio_64(IntPtr p_instance, ref audio_frame_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_audio", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_audio_32(IntPtr p_instance, ref audio_frame_t p_audio_data);

			// send_send_video 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_video", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_64(IntPtr p_instance, ref video_frame_t p_video_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_video", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_32(IntPtr p_instance, ref video_frame_t p_video_data);

			// send_send_video_async 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_video_async", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_async_64(IntPtr p_instance, ref video_frame_t p_video_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_video_async", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_async_32(IntPtr p_instance, ref video_frame_t p_video_data);

			// send_send_audio 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_audio", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_audio_64(IntPtr p_instance, ref audio_frame_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_audio", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_audio_32(IntPtr p_instance, ref audio_frame_t p_audio_data);

			// util_audio_to_interleaved_16s 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_16s_64(ref audio_frame_t p_src, ref audio_frame_interleaved_16s_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_16s_32(ref audio_frame_t p_src, ref audio_frame_interleaved_16s_t p_dst);

			// util_audio_from_interleaved_16s 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_16s_64(ref audio_frame_interleaved_16s_t p_src, ref audio_frame_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_16s_32(ref audio_frame_interleaved_16s_t p_src, ref audio_frame_t p_dst);

			// util_audio_to_interleaved_32f 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_32f_64(ref audio_frame_t p_src, ref audio_frame_interleaved_32f_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_32f_32(ref audio_frame_t p_src, ref audio_frame_interleaved_32f_t p_dst);

			// util_audio_from_interleaved_32f 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_32f_64(ref audio_frame_interleaved_32f_t p_src, ref audio_frame_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_32f_32(ref audio_frame_interleaved_32f_t p_src, ref audio_frame_t p_dst);

		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

