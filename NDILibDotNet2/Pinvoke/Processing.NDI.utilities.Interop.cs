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
		// This describes an audio frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct audio_frame_interleaved_16s_t
		{
			// The sample-rate of this buffer
			public int	sample_rate;

			// The number of audio channels
			public int	no_channels;

			// The number of audio samples per channel
			public int	no_samples;

			// The timecode of this frame in 100ns intervals
			public Int64	timecode;

			// The audio reference level in dB. This specifies how many dB above the reference level (+4dBU) is the full range of 16 bit audio.
			// If you do not understand this and want to just use numbers :
			//		-	If you are sending audio, specify +0dB. Most common applications produce audio at reference level.
			//		-	If receiving audio, specify +20dB. This means that the full 16 bit range corresponds to professional level audio with 20dB of headroom. Note that
			//			if you are writing it into a file it might sound soft because you have 20dB of headroom before clipping.
			public int	reference_level;

			// The audio data, interleaved 16bpp
			public IntPtr	p_data;
		}

		// This describes an audio frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct audio_frame_interleaved_32f_t
		{
			// The sample-rate of this buffer
			public int	sample_rate;

			// The number of audio channels
			public int	no_channels;

			// The number of audio samples per channel
			public int	no_samples;

			// The timecode of this frame in 100ns intervals
			public Int64	timecode;

			// The audio data, interleaved 32bpp
			public IntPtr	p_data;
		}

		// This describes an audio frame
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct audio_frame_interleaved_32s_t
		{
			// The sample-rate of this buffer
			public int sample_rate;

			// The number of audio channels
			public int no_channels;

			// The number of audio samples per channel
			public int no_samples;

			// The timecode of this frame in 100ns intervals
			public Int64 timecode;

			// The audio data, interleaved 32bpp (Int32)
			public IntPtr p_data;
		}

		// This will add an audio frame in 16bpp
		public static void util_send_send_audio_interleaved_16s(IntPtr p_instance, ref audio_frame_interleaved_16s_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_send_send_audio_interleaved_16s_64( p_instance, ref p_audio_data);
			else
				 UnsafeNativeMethods.util_send_send_audio_interleaved_16s_32( p_instance, ref p_audio_data);
		}

		// This will add an audio frame interleaved floating point
		public static void util_send_send_audio_interleaved_32f(IntPtr p_instance, ref audio_frame_interleaved_32f_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_send_send_audio_interleaved_32f_64( p_instance, ref p_audio_data);
			else
				 UnsafeNativeMethods.util_send_send_audio_interleaved_32f_32( p_instance, ref p_audio_data);
		}

		// This will add an audio frame interleaved Int32
		public static void util_send_send_audio_interleaved_32s(IntPtr p_instance, ref audio_frame_interleaved_32s_t p_audio_data)
		{
			if (IntPtr.Size == 8)
				UnsafeNativeMethods.util_send_send_audio_interleaved_32s_64(p_instance, ref p_audio_data);
			else
				UnsafeNativeMethods.util_send_send_audio_interleaved_32s_32(p_instance, ref p_audio_data);
		}

		public static void util_audio_to_interleaved_16s_v2(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_16s_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_to_interleaved_16s_v2_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_to_interleaved_16s_v2_32(ref p_src, ref p_dst);
		}

		public static void util_audio_from_interleaved_16s_v2(ref audio_frame_interleaved_16s_t p_src, ref audio_frame_v2_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_from_interleaved_16s_v2_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_from_interleaved_16s_v2_32(ref p_src, ref p_dst);
		}

		public static void util_audio_to_interleaved_32f_v2(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_32f_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_to_interleaved_32f_v2_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_to_interleaved_32f_v2_32(ref p_src, ref p_dst);
		}

		public static void util_audio_from_interleaved_32f_v2(ref audio_frame_interleaved_32f_t p_src, ref audio_frame_v2_t p_dst)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.util_audio_from_interleaved_32f_v2_64(ref p_src, ref p_dst);
			else
				 UnsafeNativeMethods.util_audio_from_interleaved_32f_v2_32(ref p_src, ref p_dst);
		}

		public static void util_audio_to_interleaved_32s_v2(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_32s_t p_dst)
		{
			if (IntPtr.Size == 8)
				UnsafeNativeMethods.util_audio_to_interleaved_32s_v2_64(ref p_src, ref p_dst);
			else
				UnsafeNativeMethods.util_audio_to_interleaved_32s_v2_32(ref p_src, ref p_dst);
		}

		public static void util_audio_from_interleaved_32s_v2(ref audio_frame_interleaved_32s_t p_src, ref audio_frame_v2_t p_dst)
		{
			if (IntPtr.Size == 8)
				UnsafeNativeMethods.util_audio_from_interleaved_32s_v2_64(ref p_src, ref p_dst);
			else
				UnsafeNativeMethods.util_audio_from_interleaved_32s_v2_32(ref p_src, ref p_dst);
		}

		// This is a helper function that you may use to convert from 10bit packed UYVY into 16bit semi-planar. The FourCC on the source 
		// is ignored in this function since we do not define a V210 format in NDI. You must make sure that there is memory and a stride
		// allocated in p_dst.
		public static void util_V210_to_P216(ref video_frame_v2_t p_src_v210, ref video_frame_v2_t p_dst_p216)
		{
			if (IntPtr.Size == 8)
				UnsafeNativeMethods.util_V210_to_P216_64(ref p_src_v210, ref p_dst_p216);
			else
				UnsafeNativeMethods.util_V210_to_P216_32(ref p_src_v210, ref p_dst_p216);
		}

		// This converts from 16bit semi-planar to 10bit. You must make sure that there is memory and a stride allocated in p_dst.
		public static void util_P216_to_V210(ref video_frame_v2_t p_src_p216, ref video_frame_v2_t p_dst_v210)
		{
			if (IntPtr.Size == 8)
				UnsafeNativeMethods.util_P216_to_V210_64(ref p_src_p216, ref p_dst_v210);
			else
				UnsafeNativeMethods.util_P216_to_V210_32(ref p_src_p216, ref p_dst_v210);
		}



		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// util_send_send_audio_interleaved_16s 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_send_send_audio_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_send_send_audio_interleaved_16s_64(IntPtr p_instance, ref audio_frame_interleaved_16s_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_send_send_audio_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_send_send_audio_interleaved_16s_32(IntPtr p_instance, ref audio_frame_interleaved_16s_t p_audio_data);

			// util_send_send_audio_interleaved_32f 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_send_send_audio_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_send_send_audio_interleaved_32f_64(IntPtr p_instance, ref audio_frame_interleaved_32f_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_send_send_audio_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_send_send_audio_interleaved_32f_32(IntPtr p_instance, ref audio_frame_interleaved_32f_t p_audio_data);

			// util_send_send_audio_interleaved_32s (Int32) 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_send_send_audio_interleaved_32s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_send_send_audio_interleaved_32s_64(IntPtr p_instance, ref audio_frame_interleaved_32s_t p_audio_data);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_send_send_audio_interleaved_32s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_send_send_audio_interleaved_32s_32(IntPtr p_instance, ref audio_frame_interleaved_32s_t p_audio_data);

			// util_audio_to_interleaved_16s_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_16s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_16s_v2_64(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_16s_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_16s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_16s_v2_32(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_16s_t p_dst);

			// util_audio_from_interleaved_16s_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_16s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_16s_v2_64(ref audio_frame_interleaved_16s_t p_src, ref audio_frame_v2_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_16s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_16s_v2_32(ref audio_frame_interleaved_16s_t p_src, ref audio_frame_v2_t p_dst);

			// util_audio_to_interleaved_32f_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_32f_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_32f_v2_64(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_32f_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_32f_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_32f_v2_32(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_32f_t p_dst);

			// util_audio_from_interleaved_32f_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_32f_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_32f_v2_64(ref audio_frame_interleaved_32f_t p_src, ref audio_frame_v2_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_32f_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_32f_v2_32(ref audio_frame_interleaved_32f_t p_src, ref audio_frame_v2_t p_dst);

			// util_audio_to_interleaved_32s_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_32s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_32s_v2_64(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_32s_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_to_interleaved_32s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_to_interleaved_32s_v2_32(ref audio_frame_v2_t p_src, ref audio_frame_interleaved_32s_t p_dst);

			// util_audio_from_interleaved_32s_v2 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_32s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_32s_v2_64(ref audio_frame_interleaved_32s_t p_src, ref audio_frame_v2_t p_dst);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_audio_from_interleaved_32s_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_audio_from_interleaved_32s_v2_32(ref audio_frame_interleaved_32s_t p_src, ref audio_frame_v2_t p_dst);

			// This is a helper function that you may use to convert from 10bit packed UYVY into 16bit semi-planar. The FourCC on the source 
			// is ignored in this function since we do not define a V210 format in NDI. You must make sure that there is memory and a stride
			// allocated in p_dst.
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_V210_to_P216", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_V210_to_P216_64(ref video_frame_v2_t p_src, ref video_frame_v2_t p_dst_p216);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_V210_to_P216", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_V210_to_P216_32(ref video_frame_v2_t p_src, ref video_frame_v2_t p_dst_p216);

			// This converts from 16bit semi-planar to 10bit. You must make sure that there is memory and a stride allocated in p_dst.
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_util_P216_to_V210", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_P216_to_V210_64(ref video_frame_v2_t p_src, ref video_frame_v2_t p_dst_p216);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_util_P216_to_V210", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void util_P216_to_V210_32(ref video_frame_v2_t p_src, ref video_frame_v2_t p_dst_p216);


		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

