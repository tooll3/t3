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
		// In order to get the duration
		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct recv_recording_time_t
		{
			// The number of actual video frames recorded.
			public Int64	no_frames;

			// The starting time and current largest time of the record, in UTC time, at 100ns unit intervals. This allows you to know the record
			// time irrespective of frame-rate. For instance, last_time - start_time woudl give you the recording length in 100ns intervals.
			public Int64 start_time,	last_time;
		}

		//****************************************************************************************************************************************************
		// Has this receiver got PTZ control. Note that it might take a second or two after the connection for this value to be set.
		// To avoid the need to poll this function, you can know when the value of this function might have changed when the
		// NDILib_recv_capture* call would return NDIlib_frame_type_status_change
		public static bool recv_ptz_is_supported(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_is_supported_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_is_supported_32( p_instance);
		}

		// Has this receiver got recording control. Note that it might take a second or two after the connection for this value to be set.
		// To avoid the need to poll this function, you can know when the value of this function might have changed when the
		// NDILib_recv_capture* call would return NDIlib_frame_type_status_change
		public static bool recv_recording_is_supported(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_is_supported_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_recording_is_supported_32( p_instance);
		}

		//****************************************************************************************************************************************************
		// PTZ Controls
		// Zoom to an absolute value.
		// zoom_value = 0.0 (zoomed in) ... 1.0 (zoomed out)
		public static bool recv_ptz_zoom(IntPtr p_instance, float zoom_value)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_zoom_64( p_instance,  zoom_value);
			else
				return  UnsafeNativeMethods.recv_ptz_zoom_32( p_instance,  zoom_value);
		}

		// Zoom at a particular speed
		// zoom_speed = -1.0 (zoom outwards) ... +1.0 (zoom inwards)
		public static bool recv_ptz_zoom_speed(IntPtr p_instance, float zoom_speed)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_zoom_speed_64( p_instance,  zoom_speed);
			else
				return  UnsafeNativeMethods.recv_ptz_zoom_speed_32( p_instance,  zoom_speed);
		}

		// Set the pan and tilt to an absolute value
		// pan_value  = -1.0 (left) ... 0.0 (centred) ... +1.0 (right)
		// tilt_value = -1.0 (bottom) ... 0.0 (centred) ... +1.0 (top)
		public static bool recv_ptz_pan_tilt(IntPtr p_instance, float pan_value, float tilt_value)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_pan_tilt_64( p_instance,  pan_value,  tilt_value);
			else
				return  UnsafeNativeMethods.recv_ptz_pan_tilt_32( p_instance,  pan_value,  tilt_value);
		}

        // Set the pan and tilt direction and speed
        // pan_speed = -1.0 (moving right) ... 0.0 (stopped) ... +1.0 (moving left)
        // tilt_speed = -1.0 (down) ... 0.0 (stopped) ... +1.0 (moving up)
        public static bool recv_ptz_pan_tilt_speed(IntPtr p_instance, float pan_speed, float tilt_speed)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_pan_tilt_speed_64( p_instance,  pan_speed,  tilt_speed);
			else
				return  UnsafeNativeMethods.recv_ptz_pan_tilt_speed_32( p_instance,  pan_speed,  tilt_speed);
		}

		// Store the current position, focus, etc... as a preset.
		// preset_no = 0 ... 99
		public static bool recv_ptz_store_preset(IntPtr p_instance, int preset_no)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_store_preset_64( p_instance,  preset_no);
			else
				return  UnsafeNativeMethods.recv_ptz_store_preset_32( p_instance,  preset_no);
		}

		// Recall a preset, including position, focus, etc...
		// preset_no = 0 ... 99
		// speed = 0.0(as slow as possible) ... 1.0(as fast as possible) The speed at which to move to the new preset
		public static bool recv_ptz_recall_preset(IntPtr p_instance, int preset_no, float speed)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_recall_preset_64( p_instance,  preset_no,  speed);
			else
				return  UnsafeNativeMethods.recv_ptz_recall_preset_32( p_instance,  preset_no,  speed);
		}

		// Put the camera in auto-focus
		public static bool recv_ptz_auto_focus(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_auto_focus_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_auto_focus_32( p_instance);
		}

		// Focus to an absolute value.
		// focus_value = 0.0 (focussed to infinity) ... 1.0 (focussed as close as possible)
		public static bool recv_ptz_focus(IntPtr p_instance, float focus_value)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_focus_64( p_instance,  focus_value);
			else
				return  UnsafeNativeMethods.recv_ptz_focus_32( p_instance,  focus_value);
		}

		// Focus at a particular speed
		// focus_speed = -1.0 (focus outwards) ... +1.0 (focus inwards)
		public static bool recv_ptz_focus_speed(IntPtr p_instance, float focus_speed)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_focus_speed_64( p_instance,  focus_speed);
			else
				return  UnsafeNativeMethods.recv_ptz_focus_speed_32( p_instance,  focus_speed);
		}

		// Put the camera in auto white balance moce
		public static bool recv_ptz_white_balance_auto(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_white_balance_auto_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_white_balance_auto_32( p_instance);
		}

		// Put the camera in indoor white balance
		public static bool recv_ptz_white_balance_indoor(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_white_balance_indoor_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_white_balance_indoor_32( p_instance);
		}

		// Put the camera in indoor white balance
		public static bool recv_ptz_white_balance_outdoor(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_white_balance_outdoor_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_white_balance_outdoor_32( p_instance);
		}

		// Use the current brightness to automatically set the current white balance
		public static bool recv_ptz_white_balance_oneshot(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_white_balance_oneshot_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_white_balance_oneshot_32( p_instance);
		}

		// Set the manual camera white balance using the R, B values
		// red = 0.0(not red) ... 1.0(very red)
		// blue = 0.0(not blue) ... 1.0(very blue)
		public static bool recv_ptz_white_balance_manual(IntPtr p_instance, float red, float blue)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_white_balance_manual_64( p_instance,  red,  blue);
			else
				return  UnsafeNativeMethods.recv_ptz_white_balance_manual_32( p_instance,  red,  blue);
		}

		// Put the camera in auto-exposure mode
		public static bool recv_ptz_exposure_auto(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_exposure_auto_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_ptz_exposure_auto_32( p_instance);
		}

		// Manually set the camera exposure
		// exposure_level = 0.0(dark) ... 1.0(light)
		public static bool recv_ptz_exposure_manual(IntPtr p_instance, float exposure_level)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_ptz_exposure_manual_64( p_instance,  exposure_level);
			else
				return  UnsafeNativeMethods.recv_ptz_exposure_manual_32( p_instance,  exposure_level);
		}

		// Manually set the camera exposure parameters
		// iris = 0.0(dark) ... 1.0(light)
		// gain = 0.0(dark) ... 1.0(light)
		// shutter_speed = 0.0(slow) ... 1.0(fast)
		public static bool recv_ptz_exposure_manual_v2(IntPtr p_instance, float iris, float gain, float shutter_speed)
		{
			if (IntPtr.Size == 8)
				return UnsafeNativeMethods.recv_ptz_exposure_manual_v2_64(p_instance, iris, gain, shutter_speed);
			else
				return UnsafeNativeMethods.recv_ptz_exposure_manual_v2_32(p_instance, iris, gain, shutter_speed);
		}

		//****************************************************************************************************************************************************
		// Recording control
		// This will start recording.If the recorder was already recording then the message is ignored.A filename is passed in as a "hint".Since the recorder might
		// already be recording(or might not allow complete flexibility over its filename), the filename might or might not be used.If the filename is empty, or
		// not present, a name will be chosen automatically. If you do not with to provide a filename hint you can simply pass NULL.
		public static bool recv_recording_start(IntPtr p_instance, IntPtr p_filename_hint)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_start_64( p_instance,  p_filename_hint);
			else
				return  UnsafeNativeMethods.recv_recording_start_32( p_instance,  p_filename_hint);
		}

		// Stop recording.
		public static bool recv_recording_stop(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_stop_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_recording_stop_32( p_instance);
		}

		// This will control the audio level for the recording.dB is specified in decibels relative to the reference level of the source. Not all recording sources support
		// controlling audio levels.For instance, a digital audio device would not be able to avoid clipping on sources already at the wrong level, thus
		// might not support this message.
		public static bool recv_recording_set_audio_level(IntPtr p_instance, float level_dB)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_set_audio_level_64( p_instance,  level_dB);
			else
				return  UnsafeNativeMethods.recv_recording_set_audio_level_32( p_instance,  level_dB);
		}

		// This will determine if the source is currently recording. It will return true while recording is in progress and false when it is not. Because there is
		// one recorded and multiple people might be connected to it, there is a chance that it is recording which was initiated by someone else.
		public static bool recv_recording_is_recording(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_is_recording_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_recording_is_recording_32( p_instance);
		}

		// Get the current filename for recording. When this is set it will return a non-NULL value which is owned by you and freed using NDIlib_recv_free_string.
		// If a file was already being recorded by another client, the massage will contain the name of that file. The filename contains a UNC path (when one is available)
		// to the recorded file, and can be used to access the file on your local machine for playback.  If a UNC path is not available, then this will represent the local
		// filename. This will remain valid even after the file has stopped being recorded until the next file is started.
		public static IntPtr recv_recording_get_filename(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_get_filename_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_recording_get_filename_32( p_instance);
		}

		// This will tell you whether there was a recording error and what that string is. When this is set it will return a non-NULL value which is owned by you and
		// freed using NDIlib_recv_free_string. When there is no error it will return NULL.
		public static IntPtr recv_recording_get_error(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_get_error_64( p_instance);
			else
				return  UnsafeNativeMethods.recv_recording_get_error_32( p_instance);
		}

		// Get the current recording times. These remain
		public static bool recv_recording_get_times(IntPtr p_instance, ref recv_recording_time_t p_times)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.recv_recording_get_times_64( p_instance, ref p_times);
			else
				return  UnsafeNativeMethods.recv_recording_get_times_32( p_instance, ref p_times);
		}

		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// recv_ptz_is_supported 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_is_supported", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_is_supported_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_is_supported", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_is_supported_32(IntPtr p_instance);

			// recv_recording_is_supported 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_is_supported", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_is_supported_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_is_supported", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_is_supported_32(IntPtr p_instance);

			// recv_ptz_zoom 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_zoom", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_zoom_64(IntPtr p_instance, float zoom_value);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_zoom", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_zoom_32(IntPtr p_instance, float zoom_value);

			// recv_ptz_zoom_speed 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_zoom_speed", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_zoom_speed_64(IntPtr p_instance, float zoom_speed);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_zoom_speed", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_zoom_speed_32(IntPtr p_instance, float zoom_speed);

			// recv_ptz_pan_tilt 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_pan_tilt", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_pan_tilt_64(IntPtr p_instance, float pan_value, float tilt_value);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_pan_tilt", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_pan_tilt_32(IntPtr p_instance, float pan_value, float tilt_value);

			// recv_ptz_pan_tilt_speed 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_pan_tilt_speed", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_pan_tilt_speed_64(IntPtr p_instance, float pan_speed, float tilt_speed);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_pan_tilt_speed", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_pan_tilt_speed_32(IntPtr p_instance, float pan_speed, float tilt_speed);

			// recv_ptz_store_preset 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_store_preset", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_store_preset_64(IntPtr p_instance, int preset_no);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_store_preset", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_store_preset_32(IntPtr p_instance, int preset_no);

			// recv_ptz_recall_preset 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_recall_preset", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_recall_preset_64(IntPtr p_instance, int preset_no, float speed);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_recall_preset", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_recall_preset_32(IntPtr p_instance, int preset_no, float speed);

			// recv_ptz_auto_focus 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_auto_focus", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_auto_focus_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_auto_focus", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_auto_focus_32(IntPtr p_instance);

			// recv_ptz_focus 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_focus", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_focus_64(IntPtr p_instance, float focus_value);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_focus", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_focus_32(IntPtr p_instance, float focus_value);

			// recv_ptz_focus_speed 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_focus_speed", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_focus_speed_64(IntPtr p_instance, float focus_speed);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_focus_speed", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_focus_speed_32(IntPtr p_instance, float focus_speed);

			// recv_ptz_white_balance_auto 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_auto", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_auto_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_auto", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_auto_32(IntPtr p_instance);

			// recv_ptz_white_balance_indoor 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_indoor", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_indoor_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_indoor", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_indoor_32(IntPtr p_instance);

			// recv_ptz_white_balance_outdoor 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_outdoor", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_outdoor_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_outdoor", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_outdoor_32(IntPtr p_instance);

			// recv_ptz_white_balance_oneshot 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_oneshot", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_oneshot_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_oneshot", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_oneshot_32(IntPtr p_instance);

			// recv_ptz_white_balance_manual 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_manual", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_manual_64(IntPtr p_instance, float red, float blue);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_white_balance_manual", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_white_balance_manual_32(IntPtr p_instance, float red, float blue);

			// recv_ptz_exposure_auto 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_exposure_auto", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_exposure_auto_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_exposure_auto", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_exposure_auto_32(IntPtr p_instance);

			// recv_ptz_exposure_manual 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_exposure_manual", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_exposure_manual_64(IntPtr p_instance, float exposure_level);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_exposure_manual", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_exposure_manual_32(IntPtr p_instance, float exposure_level);

			// Manually set the camera exposure parameters
			// iris = 0.0(dark) ... 1.0(light)
			// gain = 0.0(dark) ... 1.0(light)
			// shutter_speed = 0.0(slow) ... 1.0(fast)
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_ptz_exposure_manual_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_exposure_manual_v2_64(IntPtr p_instance, float iris, float gain, float shutter_speed);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_ptz_exposure_manual_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_ptz_exposure_manual_v2_32(IntPtr p_instance, float iris, float gain, float shutter_speed);

			// recv_recording_start 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_start", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_start_64(IntPtr p_instance, IntPtr p_filename_hint);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_start", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_start_32(IntPtr p_instance, IntPtr p_filename_hint);

			// recv_recording_stop 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_stop", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_stop_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_stop", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_stop_32(IntPtr p_instance);

			// recv_recording_set_audio_level 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_set_audio_level", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_set_audio_level_64(IntPtr p_instance, float level_dB);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_set_audio_level", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_set_audio_level_32(IntPtr p_instance, float level_dB);

			// recv_recording_is_recording 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_is_recording", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_is_recording_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_is_recording", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_is_recording_32(IntPtr p_instance);

			// recv_recording_get_filename 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_get_filename", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_recording_get_filename_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_get_filename", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_recording_get_filename_32(IntPtr p_instance);

			// recv_recording_get_error 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_get_error", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_recording_get_error_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_get_error", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr recv_recording_get_error_32(IntPtr p_instance);

			// recv_recording_get_times 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_recording_get_times", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_get_times_64(IntPtr p_instance, ref recv_recording_time_t p_times);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_recv_recording_get_times", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool recv_recording_get_times_32(IntPtr p_instance, ref recv_recording_time_t p_times);

		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

