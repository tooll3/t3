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
		public struct routing_create_t
		{
			// The name of the NDI source to create. This is a NULL terminated UTF8 string.
			public IntPtr	p_ndi_name;

			// What groups should this source be part of
			public IntPtr	p_groups;
		}

		// Create an NDI routing source
		public static IntPtr routing_create(ref routing_create_t p_create_settings)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.routing_create_64(ref p_create_settings);
			else
				return  UnsafeNativeMethods.routing_create_32(ref p_create_settings);
		}

		// Destroy and NDI routing source
		public static void routing_destroy(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.routing_destroy_64( p_instance);
			else
				 UnsafeNativeMethods.routing_destroy_32( p_instance);
		}

		// Change the routing of this source to another destination
		public static bool routing_change(IntPtr p_instance, ref source_t p_source)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.routing_change_64( p_instance, ref p_source);
			else
				return  UnsafeNativeMethods.routing_change_32( p_instance, ref p_source);
		}

		// Change the routing of this source to another destination
		public static bool routing_clear(IntPtr p_instance)
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.routing_clear_64( p_instance);
			else
				return  UnsafeNativeMethods.routing_clear_32( p_instance);
		}

		// Get the current number of receivers connected to this source. This can be used to avoid even rendering when nothing is connected to the video source. 
		// which can significantly improve the efficiency if you want to make a lot of sources available on the network. If you specify a timeout that is not
		// 0 then it will wait until there are connections for this amount of time.
		public static int routing_clear(IntPtr p_instance, UInt32 timeout_in_ms)
		{
			if (IntPtr.Size == 8)
				return UnsafeNativeMethods.routing_get_no_connections_64(p_instance, timeout_in_ms);
			else
				return UnsafeNativeMethods.routing_get_no_connections_32(p_instance, timeout_in_ms);
		}

		// Retrieve the source information for the given router instance.
		// This can throw an ArgumentException or ArgumentNullException!
		public static source_t routing_get_source_name(IntPtr p_instance, ref source_t p_failover_source)
		{
			if (IntPtr.Size == 8)
				return (source_t)Marshal.PtrToStructure(UnsafeNativeMethods.routing_get_source_name_64(p_instance), typeof(source_t));
			else
				return (source_t)Marshal.PtrToStructure(UnsafeNativeMethods.routing_get_source_name_32(p_instance), typeof(source_t));
		}

		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// routing_create 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_routing_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr routing_create_64(ref routing_create_t p_create_settings);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_routing_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr routing_create_32(ref routing_create_t p_create_settings);

			// routing_destroy 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_routing_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void routing_destroy_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_routing_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void routing_destroy_32(IntPtr p_instance);

			// routing_change 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_routing_change", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool routing_change_64(IntPtr p_instance, ref source_t p_source);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_routing_change", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool routing_change_32(IntPtr p_instance, ref source_t p_source);

			// routing_clear 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_routing_clear", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool routing_clear_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_routing_clear", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool routing_clear_32(IntPtr p_instance);

			// Get the current number of receivers connected to this source. This can be used to avoid even rendering when nothing is connected to the video source. 
			// which can significantly improve the efficiency if you want to make a lot of sources available on the network. If you specify a timeout that is not
			// 0 then it will wait until there are connections for this amount of time.
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_routing_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int routing_get_no_connections_64(IntPtr p_instance, UInt32 timeout_in_ms);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_routing_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int routing_get_no_connections_32(IntPtr p_instance, UInt32 timeout_in_ms);

			// Retrieve the source information for the given router instance.  This pointer is valid until NDIlib_routing_destroy is called.
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_routing_get_source_name", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr routing_get_source_name_64(IntPtr p_instance);
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_routing_get_source_name", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr routing_get_source_name_32(IntPtr p_instance);

		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

