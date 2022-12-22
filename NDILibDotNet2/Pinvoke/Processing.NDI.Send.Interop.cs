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
		// The creation structure that is used when you are creating a sender
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct send_create_t
		{
			// The name of the NDI source to create. This is a NULL terminated UTF8 string.
			public IntPtr	p_ndi_name;

			// What groups should this source be part of. NULL means default.
			public IntPtr	p_groups;

			// Do you want audio and video to "clock" themselves. When they are clocked then
			// by adding video frames, they will be rate limited to match the current frame-rate
			// that you are submitting at. The same is true for audio. In general if you are submitting
			// video and audio off a single thread then you should only clock one of them (video is
			// probably the better of the two to clock off). If you are submtiting audio and video
			// of separate threads then having both clocked can be useful.
			[MarshalAsAttribute(UnmanagedType.U1)]
			public bool clock_video,	clock_audio;
		}

		// Create a new sender instance. This will return NULL if it fails.
		public static IntPtr send_create(ref send_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.send_create_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.send_create_32(ref p_create_settings);
		}

		// This will destroy an existing finder instance.
		public static void send_destroy(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_destroy_64( p_instance);
			else
				 UnsafeNativeMethods.send_destroy_32( p_instance);
		}

		// This will add a video frame
		public static void send_send_video_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_video_v2_64( p_instance, ref p_video_data);
			else
				 UnsafeNativeMethods.send_send_video_v2_32( p_instance, ref p_video_data);
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
		// Synchronozing events are :
		//		- a call to NDIlib_send_send_video
		//		- a call to NDIlib_send_send_video_async with another frame to be sent
		//		- a call to NDIlib_send_send_video with p_video_data=NULL
		//		- a call to NDIlib_send_destroy
		public static void send_send_video_async_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_video_async_v2_64( p_instance, ref p_video_data);
			else
				 UnsafeNativeMethods.send_send_video_async_v2_32( p_instance, ref p_video_data);
		}

		// This will add an audio frame
		public static void send_send_audio_v2(IntPtr p_instance, ref audio_frame_v2_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_audio_v2_64( p_instance, ref p_audio_data);
			else
				 UnsafeNativeMethods.send_send_audio_v2_32( p_instance, ref p_audio_data);
		}

        // This will add an audio frame
        public static void send_send_audio_v3(IntPtr p_instance, ref audio_frame_v3_t p_audio_data)
        {
            if (IntPtr.Size == 8)
                UnsafeNativeMethods.send_send_audio_v3_64(p_instance, ref p_audio_data);
            else
                UnsafeNativeMethods.send_send_audio_v3_32(p_instance, ref p_audio_data);
        }

        // This will add a metadata frame
        public static void send_send_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_send_metadata_64( p_instance, ref p_metadata);
			else
				 UnsafeNativeMethods.send_send_metadata_32( p_instance, ref p_metadata);
		}

		// This allows you to receive metadata from the other end of the connection
		public static frame_type_e send_capture(IntPtr p_instance, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.send_capture_64( p_instance, ref p_metadata,  timeout_in_ms);
			else
				return  UnsafeNativeMethods.send_capture_32( p_instance, ref p_metadata,  timeout_in_ms);
		}

		// Free the buffers returned by capture for metadata
		public static void send_free_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_free_metadata_64( p_instance, ref p_metadata);
			else
				 UnsafeNativeMethods.send_free_metadata_32( p_instance, ref p_metadata);
		}

		// Determine the current tally sate. If you specify a timeout then it will wait until it has changed, otherwise it will simply poll it
		// and return the current tally immediately. The return value is whether anything has actually change (true) or whether it timed out (false)
		public static bool send_get_tally(IntPtr p_instance, ref tally_t p_tally, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.send_get_tally_64( p_instance, ref p_tally,  timeout_in_ms);
			else
				return  UnsafeNativeMethods.send_get_tally_32( p_instance, ref p_tally,  timeout_in_ms);
		}

		// Get the current number of receivers connected to this source. This can be used to avoid even rendering when nothing is connected to the video source.
		// which can significantly improve the efficiency if you want to make a lot of sources available on the network. If you specify a timeout that is not
		// 0 then it will wait until there are connections for this amount of time.
		public static int send_get_no_connections(IntPtr p_instance, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.send_get_no_connections_64( p_instance,  timeout_in_ms);
			else
				return  UnsafeNativeMethods.send_get_no_connections_32( p_instance,  timeout_in_ms);
		}

		// Connection based metadata is data that is sent automatically each time a new connection is received. You queue all of these
		// up and they are sent on each connection. To reset them you need to clear them all and set them up again.
		public static void send_clear_connection_metadata(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_clear_connection_metadata_64( p_instance);
			else
				 UnsafeNativeMethods.send_clear_connection_metadata_32( p_instance);
		}

		// Add a connection metadata string to the list of what is sent on each new connection. If someone is already connected then
		// this string will be sent to them immediately.
		public static void send_add_connection_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_add_connection_metadata_64( p_instance, ref p_metadata);
			else
				 UnsafeNativeMethods.send_add_connection_metadata_32( p_instance, ref p_metadata);
		}

		// This will assign a new fail-over source for this video source. What this means is that if this video source was to fail
		// any receivers would automatically switch over to use this source, unless this source then came back online. You can specify
		// NULL to clear the source.
		public static void send_set_failover(IntPtr p_instance, ref source_t p_failover_source)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.send_set_failover_64( p_instance, ref p_failover_source);
			else
				 UnsafeNativeMethods.send_set_failover_32( p_instance, ref p_failover_source);
		}

		// Retrieve the source information for the given sender instance.
		// This can throw an ArgumentException or ArgumentNullException!
		public static source_t send_get_source_name(IntPtr p_instance, ref source_t p_failover_source)
		{
			if (IntPtr.Size == 8)
				return (source_t)Marshal.PtrToStructure(UnsafeNativeMethods.send_get_source_name_64(p_instance), typeof(source_t));
			else
				return (source_t)Marshal.PtrToStructure(UnsafeNativeMethods.send_get_source_name_32(p_instance), typeof(source_t));
		}

		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// send_create 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr send_create_64(ref send_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr send_create_32(ref send_create_t p_create_settings);

			// send_destroy 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_destroy_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_destroy_32(IntPtr p_instance);

			// send_send_video_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_video_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_v2_64(IntPtr p_instance, ref video_frame_v2_t p_video_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_video_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_v2_32(IntPtr p_instance, ref video_frame_v2_t p_video_data);

			// send_send_video_async_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_video_async_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_async_v2_64(IntPtr p_instance, ref video_frame_v2_t p_video_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_video_async_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_video_async_v2_32(IntPtr p_instance, ref video_frame_v2_t p_video_data);

			// send_send_audio_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_audio_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_audio_v2_64(IntPtr p_instance, ref audio_frame_v2_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_audio_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_audio_v2_32(IntPtr p_instance, ref audio_frame_v2_t p_audio_data);

            // send_send_audio_v3 
            [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_audio_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void send_send_audio_v3_64(IntPtr p_instance, ref audio_frame_v3_t p_audio_data);
            [DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_audio_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void send_send_audio_v3_32(IntPtr p_instance, ref audio_frame_v3_t p_audio_data);

            // send_send_metadata 
            [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_metadata_64(IntPtr p_instance, ref metadata_frame_t p_metadata);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_send_metadata_32(IntPtr p_instance, ref metadata_frame_t p_metadata);

			// send_capture 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern frame_type_e send_capture_64(IntPtr p_instance, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern frame_type_e send_capture_32(IntPtr p_instance, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);

			// send_free_metadata 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_free_metadata_64(IntPtr p_instance, ref metadata_frame_t p_metadata);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_free_metadata_32(IntPtr p_instance, ref metadata_frame_t p_metadata);

			// send_get_tally 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_get_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool send_get_tally_64(IntPtr p_instance, ref tally_t p_tally, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_get_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool send_get_tally_32(IntPtr p_instance, ref tally_t p_tally, UInt32 timeout_in_ms);

			// send_get_no_connections 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int send_get_no_connections_64(IntPtr p_instance, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int send_get_no_connections_32(IntPtr p_instance, UInt32 timeout_in_ms);

			// send_clear_connection_metadata 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_clear_connection_metadata_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_clear_connection_metadata_32(IntPtr p_instance);

			// send_add_connection_metadata 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_add_connection_metadata_64(IntPtr p_instance, ref metadata_frame_t p_metadata);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_add_connection_metadata_32(IntPtr p_instance, ref metadata_frame_t p_metadata);

			// send_set_failover 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_set_failover", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_set_failover_64(IntPtr p_instance, ref source_t p_failover_source);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_set_failover", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void send_set_failover_32(IntPtr p_instance, ref source_t p_failover_source);

			// Retrieve the source information for the given sender instance.  This pointer is valid until NDIlib_send_destroy is called.
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_send_get_source_name", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr send_get_source_name_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_send_get_source_name", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr send_get_source_name_32(IntPtr p_instance);

		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

