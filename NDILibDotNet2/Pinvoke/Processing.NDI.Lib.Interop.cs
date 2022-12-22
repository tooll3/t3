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
		public static UInt32 NDILIB_CPP_DEFAULT_CONSTRUCTORS = 0;

		// This is not actually required, but will start and end the libraries which might get
		// you slightly better performance in some cases. In general it is more "correct" to
		// call these although it is not required. There is no way to call these that would have
		// an adverse impact on anything (even calling destroy before you've deleted all your
		// objects). This will return false if the CPU is not sufficiently capable to run NDILib
		// currently NDILib requires SSE4.2 instructions (see documentation). You can verify
		// a specific CPU against the library with a call to NDIlib_is_supported_CPU()
		public static bool initialize( )
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.initialize_64( );
			else
				return  UnsafeNativeMethods.initialize_32( );
		}

		public static void destroy( )
		{
			if (IntPtr.Size == 8)
				 UnsafeNativeMethods.destroy_64( );
			else
				 UnsafeNativeMethods.destroy_32( );
		}

		public static IntPtr version( )
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.version_64( );
			else
				return  UnsafeNativeMethods.version_32( );
		}

		// Recover whether the current CPU in the system is capable of running NDILib.
		public static bool is_supported_CPU( )
		{
			if (IntPtr.Size == 8)
				return  UnsafeNativeMethods.is_supported_CPU_64( );
			else
				return  UnsafeNativeMethods.is_supported_CPU_32( );
		}

		[SuppressUnmanagedCodeSecurity]
		internal static partial class UnsafeNativeMethods
		{
			// initialize 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool initialize_64( );
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool initialize_32( );

			// destroy 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void destroy_64( );
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void destroy_32( );

			// version 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_version", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr version_64( );
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_version", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr version_32( );

			// is_supported_CPU 
			[DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_is_supported_CPU", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool is_supported_CPU_64( );
			[DllImport("Processing.NDI.Lib.x86.dll", EntryPoint = "NDIlib_is_supported_CPU", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAsAttribute(UnmanagedType.U1)]
			internal static extern bool is_supported_CPU_32( );

		} // UnsafeNativeMethods

	} // class NDIlib

} // namespace NewTek

