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
		public enum recv_bandwidth_e
		{
			// Receive metadata.
			recv_bandwidth_metadata_only = -10,

			// Receive metadata audio.
			recv_bandwidth_audio_only = 10,

			// Receive metadata audio video at a lower bandwidth and resolution.
			recv_bandwidth_lowest = 0,

			// Receive metadata audio video at full resolution.
			recv_bandwidth_highest = 100
		}

		public enum recv_color_format_e
		{
			// No alpha channel: BGRX Alpha channel: BGRA
			recv_color_format_BGRX_BGRA = 0,

			// No alpha channel: UYVY Alpha channel: BGRA
			recv_color_format_UYVY_BGRA = 1,

			// No alpha channel: RGBX Alpha channel: RGBA
			recv_color_format_RGBX_RGBA = 2,

			// No alpha channel: UYVY Alpha channel: RGBA
			recv_color_format_UYVY_RGBA = 3,

			// On Windows there are some APIs that require bottom to top images in RGBA format. Specifying
			// this format will return images in this format. The image data pointer will still point to the
			// "top" of the image, althought he stride will be negative. You can get the "bottom" line of the image
			// using : video_data.p_data + (video_data.yres - 1)*video_data.line_stride_in_bytes
			recv_color_format_BGRX_BGRA_flipped = 200,

			// Read the SDK documentation to understand the pros and cons of this format.
			recv_color_format_fastest = 100,

			// Legacy definitions for backwards compatibility
			recv_color_format_e_BGRX_BGRA = recv_color_format_BGRX_BGRA,
			recv_color_format_e_UYVY_BGRA = recv_color_format_UYVY_BGRA,
			recv_color_format_e_RGBX_RGBA = recv_color_format_RGBX_RGBA,
			recv_color_format_e_UYVY_RGBA = recv_color_format_UYVY_RGBA
		}

		// The creation structure that is used when you are creating a receiver
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct recv_create_v3_t
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

			// The name of the NDI receiver to create. This is a NULL terminated UTF8 string and should be
			// the name of receive channel that you have. This is in many ways symettric with the name of
			// senders, so this might be "Channel 1" on your system.
			public IntPtr p_ndi_recv_name;
		}

		// This allows you determine the current performance levels of the receiving to be able to detect whether frames have been dropped
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct recv_performance_t
		{
			// The number of video frames
			public Int64	video_frames;

			// The number of audio frames
			public Int64	audio_frames;

			// The number of metadata frames
			public Int64	metadata_frames;
		}

		// Get the current queue depths
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct recv_queue_t
		{
			// The number of video frames
			public int	video_frames;

			// The number of audio frames
			public int	audio_frames;

			// The number of metadata frames
			public int	metadata_frames;
		}

		//**************************************************************************************************************************
		// Create a new receiver instance. This will return NULL if it fails.
		public static IntPtr recv_create_v3(ref recv_create_v3_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_create_v3_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.recv_create_v3_32(ref p_create_settings);
		}

		// This will destroy an existing receiver instance.
		public static void recv_destroy(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_destroy_64( p_instance);
			else
				 UnsafeNativeMethods.recv_destroy_32( p_instance);
		}

		// This function allows you to change the connection to another video source, you can also disconnect it by specifying a IntPtr.Zero here. 
		// This allows you to preserve a receiver without needing to recreate it.
		public static void recv_connect(IntPtr p_instance, source_t? source)
		{
			IntPtr p_src = IntPtr.Zero;

			if (source.HasValue && source.Value.p_ndi_name != IntPtr.Zero)
			{
				// allocate room for our copy
				p_src = Marshal.AllocHGlobal(Marshal.SizeOf(source.Value));

				// copy it in
				Marshal.StructureToPtr(source.Value, p_src, false);
			}

			if (IntPtr.Size == 8)
				UnsafeNativeMethods.recv_connect_64(p_instance, p_src);
			else
				UnsafeNativeMethods.recv_connect_32(p_instance, p_src);

			// free things if needed
			if (p_src != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(p_src);
			}
		}

		// This will allow you to receive video, audio and metadata frames.
		// Any of the buffers can be NULL, in which case data of that type
		// will not be captured in this call. This call can be called simultaneously
		// on separate threads, so it is entirely possible to receive audio, video, metadata
		// all on separate threads. This function will return NDIlib_frame_type_none if no
		// data is received within the specified timeout and NDIlib_frame_type_error if the connection is lost.
		// Buffers captured with this must be freed with the appropriate free function below.
		public static frame_type_e recv_capture_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v2_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_capture_v2_64( p_instance, ref p_video_data, ref p_audio_data, ref p_metadata,  timeout_in_ms);
			else
				return  UnsafeNativeMethods.recv_capture_v2_32( p_instance, ref p_video_data, ref p_audio_data, ref p_metadata,  timeout_in_ms);
		}

        // This will allow you to receive video, audio and metadata frames.
        // Any of the buffers can be NULL, in which case data of that type
        // will not be captured in this call. This call can be called simultaneously
        // on separate threads, so it is entirely possible to receive audio, video, metadata
        // all on separate threads. This function will return NDIlib_frame_type_none if no
        // data is received within the specified timeout and NDIlib_frame_type_error if the connection is lost.
        // Buffers captured with this must be freed with the appropriate free function below.
        public static frame_type_e recv_capture_v3(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v3_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms)
        {
            if (IntPtr.Size == 8)
                return UnsafeNativeMethods.recv_capture_v3_64(p_instance, ref p_video_data, ref p_audio_data, ref p_metadata, timeout_in_ms);
            else
                return UnsafeNativeMethods.recv_capture_v3_32(p_instance, ref p_video_data, ref p_audio_data, ref p_metadata, timeout_in_ms);
        }

        // Free the buffers returned by capture for video
        public static void recv_free_video_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_free_video_v2_64( p_instance, ref p_video_data);
			else
				 UnsafeNativeMethods.recv_free_video_v2_32( p_instance, ref p_video_data);
		}

		// Free the buffers returned by capture for audio
		public static void recv_free_audio_v2(IntPtr p_instance, ref audio_frame_v2_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_free_audio_v2_64( p_instance, ref p_audio_data);
			else
				 UnsafeNativeMethods.recv_free_audio_v2_32( p_instance, ref p_audio_data);
		}

        // Free the buffers returned by capture for audio
        public static void recv_free_audio_v3(IntPtr p_instance, ref audio_frame_v3_t p_audio_data)
        {
            if (IntPtr.Size == 8)
                UnsafeNativeMethods.recv_free_audio_v3_64(p_instance, ref p_audio_data);
            else
                UnsafeNativeMethods.recv_free_audio_v3_32(p_instance, ref p_audio_data);
        }

        // Free the buffers returned by capture for metadata
        public static void recv_free_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_free_metadata_64( p_instance, ref p_metadata);
			else
				 UnsafeNativeMethods.recv_free_metadata_32( p_instance, ref p_metadata);
		}

		// This will free a string that was allocated and returned by NDIlib_recv (for instance the NDIlib_recv_get_web_control) function.
		public static void recv_free_string(IntPtr p_instance, IntPtr p_string)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_free_string_64( p_instance,  p_string);
			else
				 UnsafeNativeMethods.recv_free_string_32( p_instance,  p_string);
		}

		// This function will send a meta message to the source that we are connected too. This returns FALSE if we are
		// not currently connected to anything.
		public static bool recv_send_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_send_metadata_64( p_instance, ref p_metadata);
			else
				return  UnsafeNativeMethods.recv_send_metadata_32( p_instance, ref p_metadata);
		}

		// Set the up-stream tally notifications. This returns FALSE if we are not currently connected to anything. That
		// said, the moment that we do connect to something it will automatically be sent the tally state.
		public static bool recv_set_tally(IntPtr p_instance, ref tally_t p_tally)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_set_tally_64( p_instance, ref p_tally);
			else
				return  UnsafeNativeMethods.recv_set_tally_32( p_instance, ref p_tally);
		}

		// Get the current performance structures. This can be used to determine if you have been calling NDIlib_recv_capture fast
		// enough, or if your processing of data is not keeping up with real-time. The total structure will give you the total frame
		// counts received, the dropped structure will tell you how many frames have been dropped. Either of these could be NULL.
		public static void recv_get_performance(IntPtr p_instance, ref recv_performance_t p_total, ref recv_performance_t p_dropped)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_get_performance_64( p_instance, ref p_total, ref p_dropped);
			else
				 UnsafeNativeMethods.recv_get_performance_32( p_instance, ref p_total, ref p_dropped);
		}

		// This will allow you to determine the current queue depth for all of the frame sources at any time.
		public static void recv_get_queue(IntPtr p_instance, ref recv_queue_t p_total)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_get_queue_64( p_instance, ref p_total);
			else
				 UnsafeNativeMethods.recv_get_queue_32( p_instance, ref p_total);
		}

		// Connection based metadata is data that is sent automatically each time a new connection is received. You queue all of these
		// up and they are sent on each connection. To reset them you need to clear them all and set them up again.
		public static void recv_clear_connection_metadata(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_clear_connection_metadata_64( p_instance);
			else
				 UnsafeNativeMethods.recv_clear_connection_metadata_32( p_instance);
		}

		// Add a connection metadata string to the list of what is sent on each new connection. If someone is already connected then
		// this string will be sent to them immediately.
		public static void recv_add_connection_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.recv_add_connection_metadata_64( p_instance, ref p_metadata);
			else
				 UnsafeNativeMethods.recv_add_connection_metadata_32( p_instance, ref p_metadata);
		}

		// Is this receiver currently connected to a source on the other end, or has the source not yet been found or is no longe ronline.
		// This will normally return 0 or 1
		public static int recv_get_no_connections(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_get_no_connections_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_get_no_connections_32( p_instance);
		}

		// Get the URL that might be used for configuration of this input. Note that it might take a second or two after the connection for
		// this value to be set. This function will return NULL if there is no web control user interface. You should call NDIlib_recv_free_string
		// to free the string that is returned by this function. The returned value will be a fully formed URL, for instamce "http://10.28.1.192/configuration/"
		// To avoid the need to poll this function, you can know when the value of this function might have changed when the
		// NDILib_recv_capture* call would return NDIlib_frame_type_status_change
		public static IntPtr recv_get_web_control(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_get_web_control_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_get_web_control_32( p_instance);
		}

		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// recv_create_v3 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_create_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create_v3_64(ref recv_create_v3_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_create_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_create_v3_32(ref recv_create_v3_t p_create_settings);

			// recv_destroy 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_destroy_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_destroy_32(IntPtr p_instance);

			// This function allows you to change the connection to another video source, you can also disconnect it by specifying a IntPtr.Zero here. 
			// This allows you to preserve a receiver without needing to recreate it.
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_connect", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_connect_64(IntPtr p_instance, IntPtr p_src);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_connect", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_connect_32(IntPtr p_instance, IntPtr p_src);


			// recv_capture_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_capture_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern frame_type_e recv_capture_v2_64(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v2_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_capture_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern frame_type_e recv_capture_v2_32(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v2_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);

            // recv_capture_v3 
            [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_capture_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern frame_type_e recv_capture_v3_64(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v3_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);
            [DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_capture_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern frame_type_e recv_capture_v3_32(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v3_t p_audio_data, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);

            // recv_free_video_v2 
            [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_video_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_video_v2_64(IntPtr p_instance, ref video_frame_v2_t p_video_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_video_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_video_v2_32(IntPtr p_instance, ref video_frame_v2_t p_video_data);

			// recv_free_audio_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_audio_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_audio_v2_64(IntPtr p_instance, ref audio_frame_v2_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_audio_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_audio_v2_32(IntPtr p_instance, ref audio_frame_v2_t p_audio_data);

            // recv_free_audio_v3 
            [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_audio_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void recv_free_audio_v3_64(IntPtr p_instance, ref audio_frame_v3_t p_audio_data);
            [DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_audio_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void recv_free_audio_v3_32(IntPtr p_instance, ref audio_frame_v3_t p_audio_data);

            // recv_free_metadata 
            [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_metadata_64(IntPtr p_instance, ref metadata_frame_t p_metadata);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_metadata_32(IntPtr p_instance, ref metadata_frame_t p_metadata);

			// recv_free_string 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_free_string", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_string_64(IntPtr p_instance, IntPtr p_string);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_free_string", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_free_string_32(IntPtr p_instance, IntPtr p_string);

			// recv_send_metadata 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_send_metadata_64(IntPtr p_instance, ref metadata_frame_t p_metadata);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_send_metadata_32(IntPtr p_instance, ref metadata_frame_t p_metadata);

			// recv_set_tally 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_set_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_set_tally_64(IntPtr p_instance, ref tally_t p_tally);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_set_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_set_tally_32(IntPtr p_instance, ref tally_t p_tally);

			// recv_get_performance 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_get_performance", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_get_performance_64(IntPtr p_instance, ref recv_performance_t p_total, ref recv_performance_t p_dropped);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_get_performance", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_get_performance_32(IntPtr p_instance, ref recv_performance_t p_total, ref recv_performance_t p_dropped);

			// recv_get_queue 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_get_queue", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_get_queue_64(IntPtr p_instance, ref recv_queue_t p_total);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_get_queue", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_get_queue_32(IntPtr p_instance, ref recv_queue_t p_total);

			// recv_clear_connection_metadata 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_clear_connection_metadata_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_clear_connection_metadata_32(IntPtr p_instance);

			// recv_add_connection_metadata 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_add_connection_metadata_64(IntPtr p_instance, ref metadata_frame_t p_metadata);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void recv_add_connection_metadata_32(IntPtr p_instance, ref metadata_frame_t p_metadata);

			// recv_get_no_connections 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int recv_get_no_connections_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int recv_get_no_connections_32(IntPtr p_instance);

			// recv_get_web_control 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_get_web_control", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_get_web_control_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_get_web_control", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_get_web_control_32(IntPtr p_instance);

		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

