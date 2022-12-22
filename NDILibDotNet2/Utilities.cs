using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

// Utility functions outside of the NDILib SDK itself,
// but useful for working with NDI from managed languages.

namespace NewTek.NDI
{
    [SuppressUnmanagedCodeSecurity]
    public static partial class UTF
    {
        // This REQUIRES you to use Marshal.FreeHGlobal() on the returned pointer!
        public static IntPtr StringToUtf8(String managedString)
        {
            int len = Encoding.UTF8.GetByteCount(managedString);

            byte[] buffer = new byte[len + 1];

            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);

            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

            return nativeUtf8;
        }

        // this version will also return the length of the utf8 string
        // This REQUIRES you to use Marshal.FreeHGlobal() on the returned pointer!
        public static IntPtr StringToUtf8(String managedString, out int utf8Length)
        {
            utf8Length = Encoding.UTF8.GetByteCount(managedString);

            byte[] buffer = new byte[utf8Length + 1];

            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);

            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

            return nativeUtf8;
        }

        // Length is optional, but recommended
        // This is all potentially dangerous
        public static string Utf8ToString(IntPtr nativeUtf8, uint? length = null)
        {
            if (nativeUtf8 == IntPtr.Zero)
                return String.Empty;

            uint len = 0;

            if (length.HasValue)
            {
                len = length.Value;
            }
            else
            {
                // try to find the terminator
                while (Marshal.ReadByte(nativeUtf8, (int)len) != 0)
                {
                    ++len;
                }
            }

            byte[] buffer = new byte[len];

            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(buffer);
        }

    } // class NDILib

} // namespace NewTek.NDI
