using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Spout.NET2")]

public unsafe partial class SHELLEXECUTEINFOA
{
    [StructLayout(LayoutKind.Explicit, Size = 112)]
    public partial struct __Internal
    {
        [FieldOffset(0)]
        internal uint cbSize;

        [FieldOffset(4)]
        internal uint fMask;

        [FieldOffset(8)]
        internal global::System.IntPtr hwnd;

        [FieldOffset(16)]
        internal global::System.IntPtr lpVerb;

        [FieldOffset(24)]
        internal global::System.IntPtr lpFile;

        [FieldOffset(32)]
        internal global::System.IntPtr lpParameters;

        [FieldOffset(40)]
        internal global::System.IntPtr lpDirectory;

        [FieldOffset(48)]
        internal int nShow;

        [FieldOffset(56)]
        internal global::System.IntPtr hInstApp;

        [FieldOffset(64)]
        internal global::System.IntPtr lpIDList;

        [FieldOffset(72)]
        internal global::System.IntPtr lpClass;

        [FieldOffset(80)]
        internal global::System.IntPtr hkeyClass;

        [FieldOffset(88)]
        internal uint dwHotKey;

        [FieldOffset(96)]
        internal global::SHELLEXECUTEINFOA._0.__Internal _0;

        [FieldOffset(104)]
        internal global::System.IntPtr hProcess;
    }

    public unsafe partial struct _0
    {
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::System.IntPtr hIcon;

            [FieldOffset(0)]
            internal global::System.IntPtr hMonitor;
        }
    }
}

namespace Std
{
}

namespace Std
{
    namespace CompressedPair
    {
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public unsafe partial struct __Internalc__N_std_S__Compressed_pair____N_std_S_allocator__C___N_std_S__String_val____N_std_S__Simple_types__C_Vb1
        {
            [FieldOffset(0)]
            internal global::Std.StringVal.__Internalc__N_std_S__String_val____N_std_S__Simple_types__C _Myval2;
        }
    }

    namespace Allocator
    {
        [StructLayout(LayoutKind.Explicit, Size = 0)]
        public unsafe partial struct __Internal
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0?$allocator@D@std@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctorc__N_std_S_allocator__C(global::System.IntPtr __instance);
        }
    }

    public unsafe partial class Allocator<_Ty> : IDisposable
    {
        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Std.Allocator<_Ty>> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Std.Allocator<_Ty>>();

        protected bool __ownsNativeInstance;

        internal static global::Std.Allocator<_Ty> __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Std.Allocator<_Ty>(native.ToPointer(), skipVTables);
        }

        internal static global::Std.Allocator<_Ty> __CreateInstance(global::Std.Allocator.__Internal native, bool skipVTables = false)
        {
            return new global::Std.Allocator<_Ty>(native, skipVTables);
        }

        private static void* __CopyValue(global::Std.Allocator.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Std.Allocator.__Internal));
            *(global::Std.Allocator.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private Allocator(global::Std.Allocator.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected Allocator(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public Allocator()
        {
            var ___Ty = typeof(_Ty);
            if (___Ty.IsAssignableFrom(typeof(sbyte)))
            {
                __Instance = Marshal.AllocHGlobal(sizeof(global::Std.Allocator.__Internal));
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
                global::Std.Allocator.__Internal.ctorc__N_std_S_allocator__C(__Instance);
                return;
            }
            throw new ArgumentOutOfRangeException("_Ty", string.Join(", ", new[] { typeof(_Ty).FullName }), "global::Std.Allocator<_Ty> maps a C++ template class and therefore it only supports a limited set of types and their subclasses: <sbyte>.");
        }

        ~Allocator()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Std.Allocator<_Ty> __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }
    }
}

namespace Std
{
    namespace BasicString
    {
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public unsafe partial struct __Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C
        {
            [FieldOffset(0)]
            internal global::Std.CompressedPair.__Internalc__N_std_S__Compressed_pair____N_std_S_allocator__C___N_std_S__String_val____N_std_S__Simple_types__C_Vb1 _Mypair;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctorc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@QEAA@XZ")]
            internal static extern void dtorc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C(global::System.IntPtr __instance, int delete);
        }
    }

    public unsafe partial class BasicString<_Elem, _Traits, _Alloc> : IDisposable
    {
        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Std.BasicString<_Elem, _Traits, _Alloc>> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Std.BasicString<_Elem, _Traits, _Alloc>>();

        protected bool __ownsNativeInstance;

        internal static global::Std.BasicString<_Elem, _Traits, _Alloc> __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Std.BasicString<_Elem, _Traits, _Alloc>(native.ToPointer(), skipVTables);
        }

        internal static global::Std.BasicString<_Elem, _Traits, _Alloc> __CreateInstance(global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C native, bool skipVTables = false)
        {
            return new global::Std.BasicString<_Elem, _Traits, _Alloc>(native, skipVTables);
        }

        private static void* __CopyValue(global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C));
            *(global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C*)ret = native;
            return ret.ToPointer();
        }

        private BasicString(global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected BasicString(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public BasicString()
        {
            var ___Elem = typeof(_Elem);
            var ___Traits = typeof(_Traits);
            var ___Alloc = typeof(_Alloc);
            if (___Elem.IsAssignableFrom(typeof(sbyte)) && ___Traits.IsAssignableFrom(typeof(global::Std.CharTraits<sbyte>)) && ___Alloc.IsAssignableFrom(typeof(global::Std.Allocator<sbyte>)))
            {
                __Instance = Marshal.AllocHGlobal(sizeof(global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C));
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
                global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C.ctorc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C(__Instance);
                return;
            }
            throw new ArgumentOutOfRangeException("_Elem, _Traits, _Alloc", string.Join(", ", new[] { typeof(_Elem).FullName, typeof(_Traits).FullName, typeof(_Alloc).FullName }), "global::Std.BasicString<_Elem, _Traits, _Alloc> maps a C++ template class and therefore it only supports a limited set of types and their subclasses: <sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>>.");
        }

        ~BasicString()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Std.BasicString<_Elem, _Traits, _Alloc> __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
            {
                var ___Elem = typeof(_Elem);
                var ___Traits = typeof(_Traits);
                var ___Alloc = typeof(_Alloc);
                if (___Elem.IsAssignableFrom(typeof(sbyte)) && ___Traits.IsAssignableFrom(typeof(global::Std.CharTraits<sbyte>)) && ___Alloc.IsAssignableFrom(typeof(global::Std.Allocator<sbyte>)))
                {
                    global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C.dtorc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C(__Instance, 0);
                    return;
                }
                throw new ArgumentOutOfRangeException("_Elem, _Traits, _Alloc", string.Join(", ", new[] { typeof(_Elem).FullName, typeof(_Traits).FullName, typeof(_Alloc).FullName }), "global::Std.BasicString<_Elem, _Traits, _Alloc> maps a C++ template class and therefore it only supports a limited set of types and their subclasses: <sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>>.");
            }
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }
    }

    namespace StringVal
    {
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public unsafe partial struct __Internalc__N_std_S__String_val____N_std_S__Simple_types__C
        {
            [FieldOffset(0)]
            internal global::Std.StringVal.Bxty.__Internal _Bx;

            [FieldOffset(16)]
            internal ulong _Mysize;

            [FieldOffset(24)]
            internal ulong _Myres;
        }

        namespace Bxty
        {
            [StructLayout(LayoutKind.Explicit, Size = 16)]
            public unsafe partial struct __Internal
            {
                [FieldOffset(0)]
                internal fixed sbyte _Buf[16];

                [FieldOffset(0)]
                internal global::System.IntPtr _Ptr;

                [FieldOffset(0)]
                internal fixed sbyte _Alias[16];
            }
        }

    }

    namespace CharTraits
    {
        [StructLayout(LayoutKind.Explicit, Size = 0)]
        public unsafe partial struct __Internal
        {
        }
    }

    public unsafe partial class CharTraits<_Elem> : IDisposable
    {
        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Std.CharTraits<_Elem>> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Std.CharTraits<_Elem>>();

        protected bool __ownsNativeInstance;

        internal static global::Std.CharTraits<_Elem> __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Std.CharTraits<_Elem>(native.ToPointer(), skipVTables);
        }

        internal static global::Std.CharTraits<_Elem> __CreateInstance(global::Std.CharTraits.__Internal native, bool skipVTables = false)
        {
            return new global::Std.CharTraits<_Elem>(native, skipVTables);
        }

        private static void* __CopyValue(global::Std.CharTraits.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Std.CharTraits.__Internal));
            *(global::Std.CharTraits.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private CharTraits(global::Std.CharTraits.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected CharTraits(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        ~CharTraits()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Std.CharTraits<_Elem> __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }
    }

    public unsafe static partial class BasicStringExtensions
    {
        [StructLayout(LayoutKind.Explicit, Size = 0)]
        public partial struct __Internal
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?assign@?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@QEAAAEAV12@QEBD@Z")]
            internal static extern global::System.IntPtr Assign(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string _Ptr);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?data@?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@QEBAPEBDXZ")]
            internal static extern global::System.IntPtr Data(global::System.IntPtr __instance);
        }

        public static global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>> Assign(this global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>> @this, string _Ptr)
        {
            var __arg0 = ReferenceEquals(@this, null) ? global::System.IntPtr.Zero : @this.__Instance;
            var __ret = __Internal.Assign(__arg0, _Ptr);
            global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>> __result0;
            if (__ret == IntPtr.Zero) __result0 = null;
            else if (global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>>.NativeToManagedMap.ContainsKey(__ret))
                __result0 = (global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>>)global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>>.NativeToManagedMap[__ret];
            else __result0 = global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>>.__CreateInstance(__ret);
            return __result0;
        }

        public static string Data(this global::Std.BasicString<sbyte, global::Std.CharTraits<sbyte>, global::Std.Allocator<sbyte>> @this)
        {
            var __arg0 = ReferenceEquals(@this, null) ? global::System.IntPtr.Zero : @this.__Instance;
            var __ret = __Internal.Data(__arg0);
            if (__ret == global::System.IntPtr.Zero)
                return default(string);
            var __retPtr = (byte*)__ret;
            int __length = 0;
            while (*(__retPtr++) != 0) __length += sizeof(byte);
            return global::System.Text.Encoding.UTF8.GetString((byte*)__ret, __length);
        }
    }
}

public enum D3DFORMAT
{
}

public enum D3D_FEATURE_LEVEL
{
}

public enum D3D_DRIVER_TYPE
{
}

namespace Std
{
}

namespace Std
{
}

public enum DXGI_FORMAT
{
}

namespace Spout.Interop
{
    public enum SpoutCreateResult
    {
        SPOUT_CREATE_FAILED = 0,
        SPOUT_CREATE_SUCCESS = 1,
        SPOUT_ALREADY_EXISTS = 2,
        SPOUT_ALREADY_CREATED = 3
    }

    public unsafe partial class SpoutSharedMemory : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::System.IntPtr m_pBuffer;

            [FieldOffset(8)]
            internal global::System.IntPtr m_hMap;

            [FieldOffset(16)]
            internal global::System.IntPtr m_hMutex;

            [FieldOffset(24)]
            internal int m_lockCount;

            [FieldOffset(32)]
            internal global::System.IntPtr m_pName;

            [FieldOffset(40)]
            internal int m_size;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SpoutSharedMemory@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SpoutSharedMemory@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1SpoutSharedMemory@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?Create@SpoutSharedMemory@@QEAA?AW4SpoutCreateResult@@PEBDH@Z")]
            internal static extern global::Spout.Interop.SpoutCreateResult Create(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int size);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?Open@SpoutSharedMemory@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool Open(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?Close@SpoutSharedMemory@@QEAAXXZ")]
            internal static extern void Close(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?Unlock@SpoutSharedMemory@@QEAAXXZ")]
            internal static extern void Unlock(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?Debug@SpoutSharedMemory@@QEAAXXZ")]
            internal static extern void Debug(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?Lock@SpoutSharedMemory@@QEAAPEADXZ")]
            internal static extern sbyte* Lock(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutSharedMemory> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutSharedMemory>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutSharedMemory __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutSharedMemory(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutSharedMemory __CreateInstance(global::Spout.Interop.SpoutSharedMemory.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutSharedMemory(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutSharedMemory.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSharedMemory.__Internal));
            *(global::Spout.Interop.SpoutSharedMemory.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutSharedMemory(global::Spout.Interop.SpoutSharedMemory.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutSharedMemory(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutSharedMemory()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSharedMemory.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutSharedMemory(global::Spout.Interop.SpoutSharedMemory _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSharedMemory.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutSharedMemory.__Internal*)__Instance) = *((global::Spout.Interop.SpoutSharedMemory.__Internal*)_0.__Instance);
        }

        ~SpoutSharedMemory()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutSharedMemory __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public global::Spout.Interop.SpoutCreateResult Create(string name, int size)
        {
            var __ret = __Internal.Create(__Instance, name, size);
            return __ret;
        }

        public bool Open(string name)
        {
            var __ret = __Internal.Open(__Instance, name);
            return __ret;
        }

        public void Close()
        {
            __Internal.Close(__Instance);
        }

        public void Unlock()
        {
            __Internal.Unlock(__Instance);
        }

        public void Debug()
        {
            __Internal.Debug(__Instance);
        }

        public sbyte* Lock
        {
            get
            {
                var __ret = __Internal.Lock(__Instance);
                return __ret;
            }
        }
    }

    public unsafe partial class SpoutMemoryShare : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::System.IntPtr senderMem;

            [FieldOffset(8)]
            internal uint m_Width;

            [FieldOffset(12)]
            internal uint m_Height;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutMemoryShare@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutMemoryShare@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1spoutMemoryShare@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateSenderMemory@spoutMemoryShare@@QEAA_NPEBDII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateSenderMemory(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UpdateSenderMemorySize@spoutMemoryShare@@QEAA_NPEBDII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UpdateSenderMemorySize(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?OpenSenderMemory@spoutMemoryShare@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool OpenSenderMemory(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CloseSenderMemory@spoutMemoryShare@@QEAAXXZ")]
            internal static extern void CloseSenderMemory(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderMemorySize@spoutMemoryShare@@QEAA_NAEAI0@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderMemorySize(global::System.IntPtr __instance, uint* width, uint* height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UnlockSenderMemory@spoutMemoryShare@@QEAAXXZ")]
            internal static extern void UnlockSenderMemory(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseSenderMemory@spoutMemoryShare@@QEAAXXZ")]
            internal static extern void ReleaseSenderMemory(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?LockSenderMemory@spoutMemoryShare@@QEAAPEAEXZ")]
            internal static extern byte* LockSenderMemory(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutMemoryShare> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutMemoryShare>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutMemoryShare __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutMemoryShare(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutMemoryShare __CreateInstance(global::Spout.Interop.SpoutMemoryShare.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutMemoryShare(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutMemoryShare.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutMemoryShare.__Internal));
            *(global::Spout.Interop.SpoutMemoryShare.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutMemoryShare(global::Spout.Interop.SpoutMemoryShare.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutMemoryShare(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutMemoryShare()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutMemoryShare.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutMemoryShare(global::Spout.Interop.SpoutMemoryShare _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutMemoryShare.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance) = *((global::Spout.Interop.SpoutMemoryShare.__Internal*)_0.__Instance);
        }

        ~SpoutMemoryShare()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutMemoryShare __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool CreateSenderMemory(string sendername, uint width, uint height)
        {
            var __ret = __Internal.CreateSenderMemory(__Instance, sendername, width, height);
            return __ret;
        }

        public bool UpdateSenderMemorySize(string sendername, uint width, uint height)
        {
            var __ret = __Internal.UpdateSenderMemorySize(__Instance, sendername, width, height);
            return __ret;
        }

        public bool OpenSenderMemory(string sendername)
        {
            var __ret = __Internal.OpenSenderMemory(__Instance, sendername);
            return __ret;
        }

        public void CloseSenderMemory()
        {
            __Internal.CloseSenderMemory(__Instance);
        }

        public bool GetSenderMemorySize(ref uint width, ref uint height)
        {
            fixed (uint* __width0 = &width)
            {
                var __arg0 = __width0;
                fixed (uint* __height1 = &height)
                {
                    var __arg1 = __height1;
                    var __ret = __Internal.GetSenderMemorySize(__Instance, __arg0, __arg1);
                    return __ret;
                }
            }
        }

        public void UnlockSenderMemory()
        {
            __Internal.UnlockSenderMemory(__Instance);
        }

        public void ReleaseSenderMemory()
        {
            __Internal.ReleaseSenderMemory(__Instance);
        }

        protected global::Spout.Interop.SpoutSharedMemory SenderMem
        {
            get
            {
                global::Spout.Interop.SpoutSharedMemory __result0;
                if (((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->senderMem == IntPtr.Zero) __result0 = null;
                else if (global::Spout.Interop.SpoutSharedMemory.NativeToManagedMap.ContainsKey(((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->senderMem))
                    __result0 = (global::Spout.Interop.SpoutSharedMemory)global::Spout.Interop.SpoutSharedMemory.NativeToManagedMap[((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->senderMem];
                else __result0 = global::Spout.Interop.SpoutSharedMemory.__CreateInstance(((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->senderMem);
                return __result0;
            }

            set
            {
                ((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->senderMem = ReferenceEquals(value, null) ? global::System.IntPtr.Zero : value.__Instance;
            }
        }

        protected uint MWidth
        {
            get
            {
                return ((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->m_Width;
            }

            set
            {
                ((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->m_Width = value;
            }
        }

        protected uint MHeight
        {
            get
            {
                return ((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->m_Height;
            }

            set
            {
                ((global::Spout.Interop.SpoutMemoryShare.__Internal*)__Instance)->m_Height = value;
            }
        }

        public byte* LockSenderMemory
        {
            get
            {
                var __ret = __Internal.LockSenderMemory(__Instance);
                return __ret;
            }
        }
    }

    public unsafe partial class SharedTextureInfo : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 280)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal uint shareHandle;

            [FieldOffset(4)]
            internal uint width;

            [FieldOffset(8)]
            internal uint height;

            [FieldOffset(12)]
            internal uint format;

            [FieldOffset(16)]
            internal uint usage;

            [FieldOffset(20)]
            internal fixed char description[128];

            [FieldOffset(276)]
            internal uint partnerId;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SharedTextureInfo@@QEAA@AEBU0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SharedTextureInfo> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SharedTextureInfo>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SharedTextureInfo __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SharedTextureInfo(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SharedTextureInfo __CreateInstance(global::Spout.Interop.SharedTextureInfo.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SharedTextureInfo(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SharedTextureInfo.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SharedTextureInfo.__Internal));
            *(global::Spout.Interop.SharedTextureInfo.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SharedTextureInfo(global::Spout.Interop.SharedTextureInfo.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SharedTextureInfo(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SharedTextureInfo(global::Spout.Interop.SharedTextureInfo _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SharedTextureInfo.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance) = *((global::Spout.Interop.SharedTextureInfo.__Internal*)_0.__Instance);
        }

        public SharedTextureInfo()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SharedTextureInfo.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        ~SharedTextureInfo()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SharedTextureInfo __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public uint ShareHandle
        {
            get
            {
                return ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->shareHandle;
            }

            set
            {
                ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->shareHandle = value;
            }
        }

        public uint Width
        {
            get
            {
                return ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->width;
            }

            set
            {
                ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->width = value;
            }
        }

        public uint Height
        {
            get
            {
                return ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->height;
            }

            set
            {
                ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->height = value;
            }
        }

        public uint Format
        {
            get
            {
                return ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->format;
            }

            set
            {
                ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->format = value;
            }
        }

        public uint Usage
        {
            get
            {
                return ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->usage;
            }

            set
            {
                ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->usage = value;
            }
        }

        public char[] Description
        {
            get
            {
                char[] __value = null;
                if (((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->description != null)
                {
                    __value = new char[128];
                    for (int i = 0; i < 128; i++)
                        __value[i] = ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->description[i];
                }
                return __value;
            }

            set
            {
                if (value != null)
                {
                    for (int i = 0; i < 128; i++)
                        ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->description[i] = value[i];
                }
            }
        }

        public uint PartnerId
        {
            get
            {
                return ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->partnerId;
            }

            set
            {
                ((global::Spout.Interop.SharedTextureInfo.__Internal*)__Instance)->partnerId = value;
            }
        }
    }

    public unsafe partial class SpoutSenderNames : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 112)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::Spout.Interop.SpoutSharedMemory.__Internal m_senderNames;

            [FieldOffset(48)]
            internal global::Spout.Interop.SpoutSharedMemory.__Internal m_activeSender;

            [FieldOffset(96)]
            internal global::System.IntPtr m_senders;

            [FieldOffset(104)]
            internal int m_MaxSenders;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutSenderNames@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutSenderNames@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1spoutSenderNames@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?RegisterSenderName@spoutSenderNames@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool RegisterSenderName(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string senderName);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseSenderName@spoutSenderNames@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReleaseSenderName(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string senderName);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FindSenderName@spoutSenderNames@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool FindSenderName(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderNameInfo@spoutSenderNames@@QEAA_NHPEADHAEAI1AEAPEAX@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderNameInfo(global::System.IntPtr __instance, int index, sbyte* sendername, int sendernameMaxSize, uint* width, uint* height, void** dxShareHandle);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderInfo@spoutSenderNames@@QEAA_NPEBDAEAI1AEAPEAXAEAK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint* width, uint* height, void** dxShareHandle, uint* dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetSenderInfo@spoutSenderNames@@QEAA_NPEBDIIPEAXK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetSenderInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint width, uint height, global::System.IntPtr dxShareHandle, uint dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?getSharedInfo@spoutSenderNames@@QEAA_NPEBDPEAUSharedTextureInfo@@@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSharedInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string SenderName, global::System.IntPtr info);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?setSharedInfo@spoutSenderNames@@QEAA_NPEBDPEAUSharedTextureInfo@@@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetSharedInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string SenderName, global::System.IntPtr info);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetActiveSender@spoutSenderNames@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetActiveSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetActiveSender@spoutSenderNames@@QEAA_NQEAD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetActiveSender(global::System.IntPtr __instance, sbyte[] Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetActiveSenderInfo@spoutSenderNames@@QEAA_NPEAUSharedTextureInfo@@@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetActiveSenderInfo(global::System.IntPtr __instance, global::System.IntPtr info);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FindActiveSender@spoutSenderNames@@QEAA_NQEADAEAI1AEAPEAXAEAK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool FindActiveSender(global::System.IntPtr __instance, sbyte[] activename, uint* width, uint* height, void** hSharehandle, uint* dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateSender@spoutSenderNames@@QEAA_NPEBDIIPEAXK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint width, uint height, global::System.IntPtr hSharehandle, uint dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UpdateSender@spoutSenderNames@@QEAA_NPEBDIIPEAXK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UpdateSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint width, uint height, global::System.IntPtr hSharehandle, uint dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckSender@spoutSenderNames@@QEAA_NPEBDAEAI1AEAPEAXAEAK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint* width, uint* height, void** hSharehandle, uint* dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FindSender@spoutSenderNames@@QEAA_NPEADAEAI1AEAPEAXAEAK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool FindSender(global::System.IntPtr __instance, sbyte* sendername, uint* width, uint* height, void** hSharehandle, uint* dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SenderDebug@spoutSenderNames@@QEAA_NPEBDH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SenderDebug(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername, int size);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateSenderSet@spoutSenderNames@@IEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateSenderSet(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?setActiveSenderName@spoutSenderNames@@IEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetActiveSenderName(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string SenderName);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?getActiveSenderName@spoutSenderNames@@IEAA_NQEAD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetActiveSenderName(global::System.IntPtr __instance, sbyte[] SenderName);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?cleanSenderSet@spoutSenderNames@@IEAAXXZ")]
            internal static extern void CleanSenderSet(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderCount@spoutSenderNames@@QEAAHXZ")]
            internal static extern int GetSenderCount(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetMaxSenders@spoutSenderNames@@QEAAHXZ")]
            internal static extern int GetMaxSenders(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetMaxSenders@spoutSenderNames@@QEAAXH@Z")]
            internal static extern void SetMaxSenders(global::System.IntPtr __instance, int maxSenders);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutSenderNames> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutSenderNames>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutSenderNames __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutSenderNames(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutSenderNames __CreateInstance(global::Spout.Interop.SpoutSenderNames.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutSenderNames(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutSenderNames.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSenderNames.__Internal));
            *(global::Spout.Interop.SpoutSenderNames.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutSenderNames(global::Spout.Interop.SpoutSenderNames.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutSenderNames(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutSenderNames()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSenderNames.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutSenderNames(global::Spout.Interop.SpoutSenderNames _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSenderNames.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance) = *((global::Spout.Interop.SpoutSenderNames.__Internal*)_0.__Instance);
        }

        ~SpoutSenderNames()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutSenderNames __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool RegisterSenderName(string senderName)
        {
            var __ret = __Internal.RegisterSenderName(__Instance, senderName);
            return __ret;
        }

        public bool ReleaseSenderName(string senderName)
        {
            var __ret = __Internal.ReleaseSenderName(__Instance, senderName);
            return __ret;
        }

        public bool FindSenderName(string Sendername)
        {
            var __ret = __Internal.FindSenderName(__Instance, Sendername);
            return __ret;
        }

        public bool GetSenderNameInfo(int index, sbyte* sendername, int sendernameMaxSize, ref uint width, ref uint height, void** dxShareHandle)
        {
            fixed (uint* __width3 = &width)
            {
                var __arg3 = __width3;
                fixed (uint* __height4 = &height)
                {
                    var __arg4 = __height4;
                    var __ret = __Internal.GetSenderNameInfo(__Instance, index, sendername, sendernameMaxSize, __arg3, __arg4, dxShareHandle);
                    return __ret;
                }
            }
        }

        public bool GetSenderInfo(string sendername, ref uint width, ref uint height, void** dxShareHandle, ref uint dwFormat)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (uint* __dwFormat4 = &dwFormat)
                    {
                        var __arg4 = __dwFormat4;
                        var __ret = __Internal.GetSenderInfo(__Instance, sendername, __arg1, __arg2, dxShareHandle, __arg4);
                        return __ret;
                    }
                }
            }
        }

        public bool SetSenderInfo(string sendername, uint width, uint height, global::System.IntPtr dxShareHandle, uint dwFormat)
        {
            var __ret = __Internal.SetSenderInfo(__Instance, sendername, width, height, dxShareHandle, dwFormat);
            return __ret;
        }

        public bool GetSharedInfo(string SenderName, global::Spout.Interop.SharedTextureInfo info)
        {
            var __arg1 = ReferenceEquals(info, null) ? global::System.IntPtr.Zero : info.__Instance;
            var __ret = __Internal.GetSharedInfo(__Instance, SenderName, __arg1);
            return __ret;
        }

        public bool SetSharedInfo(string SenderName, global::Spout.Interop.SharedTextureInfo info)
        {
            var __arg1 = ReferenceEquals(info, null) ? global::System.IntPtr.Zero : info.__Instance;
            var __ret = __Internal.SetSharedInfo(__Instance, SenderName, __arg1);
            return __ret;
        }

        public bool SetActiveSender(string Sendername)
        {
            var __ret = __Internal.SetActiveSender(__Instance, Sendername);
            return __ret;
        }

        public bool GetActiveSender(sbyte[] Sendername)
        {
            if (Sendername == null || Sendername.Length != 256)
                throw new ArgumentOutOfRangeException("Sendername", "The dimensions of the provided array don't match the required size.");
            var __ret = __Internal.GetActiveSender(__Instance, Sendername);
            return __ret;
        }

        public bool GetActiveSenderInfo(global::Spout.Interop.SharedTextureInfo info)
        {
            var __arg0 = ReferenceEquals(info, null) ? global::System.IntPtr.Zero : info.__Instance;
            var __ret = __Internal.GetActiveSenderInfo(__Instance, __arg0);
            return __ret;
        }

        public bool FindActiveSender(sbyte[] activename, ref uint width, ref uint height, void** hSharehandle, ref uint dwFormat)
        {
            if (activename == null || activename.Length != 256)
                throw new ArgumentOutOfRangeException("activename", "The dimensions of the provided array don't match the required size.");
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (uint* __dwFormat4 = &dwFormat)
                    {
                        var __arg4 = __dwFormat4;
                        var __ret = __Internal.FindActiveSender(__Instance, activename, __arg1, __arg2, hSharehandle, __arg4);
                        return __ret;
                    }
                }
            }
        }

        public bool CreateSender(string sendername, uint width, uint height, global::System.IntPtr hSharehandle, uint dwFormat)
        {
            var __ret = __Internal.CreateSender(__Instance, sendername, width, height, hSharehandle, dwFormat);
            return __ret;
        }

        public bool UpdateSender(string sendername, uint width, uint height, global::System.IntPtr hSharehandle, uint dwFormat)
        {
            var __ret = __Internal.UpdateSender(__Instance, sendername, width, height, hSharehandle, dwFormat);
            return __ret;
        }

        public bool CheckSender(string sendername, ref uint width, ref uint height, void** hSharehandle, ref uint dwFormat)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (uint* __dwFormat4 = &dwFormat)
                    {
                        var __arg4 = __dwFormat4;
                        var __ret = __Internal.CheckSender(__Instance, sendername, __arg1, __arg2, hSharehandle, __arg4);
                        return __ret;
                    }
                }
            }
        }

        public bool FindSender(sbyte* sendername, ref uint width, ref uint height, void** hSharehandle, ref uint dwFormat)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (uint* __dwFormat4 = &dwFormat)
                    {
                        var __arg4 = __dwFormat4;
                        var __ret = __Internal.FindSender(__Instance, sendername, __arg1, __arg2, hSharehandle, __arg4);
                        return __ret;
                    }
                }
            }
        }

        public bool SenderDebug(string Sendername, int size)
        {
            var __ret = __Internal.SenderDebug(__Instance, Sendername, size);
            return __ret;
        }

        protected bool CreateSenderSet()
        {
            var __ret = __Internal.CreateSenderSet(__Instance);
            return __ret;
        }

        protected bool SetActiveSenderName(string SenderName)
        {
            var __ret = __Internal.SetActiveSenderName(__Instance, SenderName);
            return __ret;
        }

        protected bool GetActiveSenderName(sbyte[] SenderName)
        {
            if (SenderName == null || SenderName.Length != 256)
                throw new ArgumentOutOfRangeException("SenderName", "The dimensions of the provided array don't match the required size.");
            var __ret = __Internal.GetActiveSenderName(__Instance, SenderName);
            return __ret;
        }

        protected void CleanSenderSet()
        {
            __Internal.CleanSenderSet(__Instance);
        }

        protected global::Spout.Interop.SpoutSharedMemory MSenderNames
        {
            get
            {
                return global::Spout.Interop.SpoutSharedMemory.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance)->m_senderNames));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance)->m_senderNames = *(global::Spout.Interop.SpoutSharedMemory.__Internal*)value.__Instance;
            }
        }

        protected global::Spout.Interop.SpoutSharedMemory MActiveSender
        {
            get
            {
                return global::Spout.Interop.SpoutSharedMemory.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance)->m_activeSender));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance)->m_activeSender = *(global::Spout.Interop.SpoutSharedMemory.__Internal*)value.__Instance;
            }
        }

        protected int MMaxSenders
        {
            get
            {
                return ((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance)->m_MaxSenders;
            }

            set
            {
                ((global::Spout.Interop.SpoutSenderNames.__Internal*)__Instance)->m_MaxSenders = value;
            }
        }

        public int SenderCount
        {
            get
            {
                var __ret = __Internal.GetSenderCount(__Instance);
                return __ret;
            }
        }

        public int MaxSenders
        {
            get
            {
                var __ret = __Internal.GetMaxSenders(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetMaxSenders(__Instance, value);
            }
        }
    }

    public unsafe partial class SpoutDirectX : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal byte bUseAccessLocks;

            [FieldOffset(4)]
            internal int g_AdapterIndex;

            [FieldOffset(8)]
            internal global::System.IntPtr g_pAdapterDX11;

            [FieldOffset(16)]
            internal global::System.IntPtr g_pImmediateContext;

            [FieldOffset(24)]
            internal global::D3D_DRIVER_TYPE g_driverType;

            [FieldOffset(28)]
            internal global::D3D_FEATURE_LEVEL g_featureLevel;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutDirectX@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutDirectX@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1spoutDirectX@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterName@spoutDirectX@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterName(global::System.IntPtr __instance, int index, sbyte* adaptername, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetAdapter@spoutDirectX@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetAdapter(global::System.IntPtr __instance, int index);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterInfo@spoutDirectX@@QEAA_NPEAD0H@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterInfo(global::System.IntPtr __instance, sbyte* renderdescription, sbyte* displaydescription, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FindNVIDIA@spoutDirectX@@QEAA_NAEAH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool FindNVIDIA(global::System.IntPtr __instance, int* nAdapter);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadDwordFromRegistry@spoutDirectX@@QEAA_NPEAKPEBD1@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadDwordFromRegistry(global::System.IntPtr __instance, uint* pValue, [MarshalAs(UnmanagedType.LPUTF8Str)] string subkey, [MarshalAs(UnmanagedType.LPUTF8Str)] string valuename);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteDwordToRegistry@spoutDirectX@@QEAA_NKPEBD0@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteDwordToRegistry(global::System.IntPtr __instance, uint dwValue, [MarshalAs(UnmanagedType.LPUTF8Str)] string subkey, [MarshalAs(UnmanagedType.LPUTF8Str)] string valuename);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateAccessMutex@spoutDirectX@@QEAA_NPEBDAEAPEAX@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateAccessMutex(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, void** hAccessMutex);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CloseAccessMutex@spoutDirectX@@QEAAXAEAPEAX@Z")]
            internal static extern void CloseAccessMutex(global::System.IntPtr __instance, void** hAccessMutex);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckAccess@spoutDirectX@@QEAA_NPEAX@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckAccess(global::System.IntPtr __instance, global::System.IntPtr hAccessMutex);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?AllowAccess@spoutDirectX@@QEAAXPEAX@Z")]
            internal static extern void AllowAccess(global::System.IntPtr __instance, global::System.IntPtr hAccessMutex);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetNumAdapters@spoutDirectX@@QEAAHXZ")]
            internal static extern int GetNumAdapters(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapter@spoutDirectX@@QEAAHXZ")]
            internal static extern int GetAdapter(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutDirectX> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutDirectX>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutDirectX __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutDirectX(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutDirectX __CreateInstance(global::Spout.Interop.SpoutDirectX.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutDirectX(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutDirectX.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutDirectX.__Internal));
            *(global::Spout.Interop.SpoutDirectX.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutDirectX(global::Spout.Interop.SpoutDirectX.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutDirectX(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutDirectX()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutDirectX.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutDirectX(global::Spout.Interop.SpoutDirectX _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutDirectX.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutDirectX.__Internal*)__Instance) = *((global::Spout.Interop.SpoutDirectX.__Internal*)_0.__Instance);
        }

        ~SpoutDirectX()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutDirectX __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool GetAdapterName(int index, sbyte* adaptername, int maxchars)
        {
            var __ret = __Internal.GetAdapterName(__Instance, index, adaptername, maxchars);
            return __ret;
        }

        public bool SetAdapter(int index)
        {
            var __ret = __Internal.SetAdapter(__Instance, index);
            return __ret;
        }

        public bool GetAdapterInfo(sbyte* renderdescription, sbyte* displaydescription, int maxchars)
        {
            var __ret = __Internal.GetAdapterInfo(__Instance, renderdescription, displaydescription, maxchars);
            return __ret;
        }

        public bool FindNVIDIA(ref int nAdapter)
        {
            fixed (int* __nAdapter0 = &nAdapter)
            {
                var __arg0 = __nAdapter0;
                var __ret = __Internal.FindNVIDIA(__Instance, __arg0);
                return __ret;
            }
        }

        public bool ReadDwordFromRegistry(ref uint pValue, string subkey, string valuename)
        {
            fixed (uint* __pValue0 = &pValue)
            {
                var __arg0 = __pValue0;
                var __ret = __Internal.ReadDwordFromRegistry(__Instance, __arg0, subkey, valuename);
                return __ret;
            }
        }

        public bool WriteDwordToRegistry(uint dwValue, string subkey, string valuename)
        {
            var __ret = __Internal.WriteDwordToRegistry(__Instance, dwValue, subkey, valuename);
            return __ret;
        }

        public bool CreateAccessMutex(string name, void** hAccessMutex)
        {
            var __ret = __Internal.CreateAccessMutex(__Instance, name, hAccessMutex);
            return __ret;
        }

        public void CloseAccessMutex(void** hAccessMutex)
        {
            __Internal.CloseAccessMutex(__Instance, hAccessMutex);
        }

        public bool CheckAccess(global::System.IntPtr hAccessMutex)
        {
            var __ret = __Internal.CheckAccess(__Instance, hAccessMutex);
            return __ret;
        }

        public void AllowAccess(global::System.IntPtr hAccessMutex)
        {
            __Internal.AllowAccess(__Instance, hAccessMutex);
        }

        public bool BUseAccessLocks
        {
            get
            {
                return ((global::Spout.Interop.SpoutDirectX.__Internal*)__Instance)->bUseAccessLocks != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutDirectX.__Internal*)__Instance)->bUseAccessLocks = (byte)(value ? 1 : 0);
            }
        }

        protected int GAdapterIndex
        {
            get
            {
                return ((global::Spout.Interop.SpoutDirectX.__Internal*)__Instance)->g_AdapterIndex;
            }

            set
            {
                ((global::Spout.Interop.SpoutDirectX.__Internal*)__Instance)->g_AdapterIndex = value;
            }
        }

        public int NumAdapters
        {
            get
            {
                var __ret = __Internal.GetNumAdapters(__Instance);
                return __ret;
            }
        }

        public int Adapter
        {
            get
            {
                var __ret = __Internal.GetAdapter(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetAdapter(__Instance, value);
            }
        }
    }

    public unsafe partial class SpoutCopy : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 3)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal byte m_bSSE2;

            [FieldOffset(1)]
            internal byte m_bSSE3;

            [FieldOffset(2)]
            internal byte m_bSSSE3;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutCopy@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutCopy@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1spoutCopy@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CopyPixels@spoutCopy@@QEAAXPEBEPEAEIII_N@Z")]
            internal static extern void CopyPixels(global::System.IntPtr __instance, byte* src, byte* dst, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FlipBuffer@spoutCopy@@QEAA_NPEBEPEAEIII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool FlipBuffer(global::System.IntPtr __instance, byte* src, byte* dst, uint width, uint height, uint glFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?memcpy_sse2@spoutCopy@@QEAAXPEAX0_K@Z")]
            internal static extern void MemcpySse2(global::System.IntPtr __instance, global::System.IntPtr dst, global::System.IntPtr src, ulong size);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgba2bgra@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Rgba2bgra(global::System.IntPtr __instance, global::System.IntPtr rgba_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?bgra2rgba@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Bgra2rgba(global::System.IntPtr __instance, global::System.IntPtr bgra_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgba_bgra@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void RgbaBgra(global::System.IntPtr __instance, global::System.IntPtr rgba_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgba_bgra_sse2@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void RgbaBgraSse2(global::System.IntPtr __instance, global::System.IntPtr rgba_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgba_bgra_ssse3@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void RgbaBgraSsse3(global::System.IntPtr __instance, global::System.IntPtr rgba_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgb2rgba@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Rgb2rgba(global::System.IntPtr __instance, global::System.IntPtr rgb_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?bgr2rgba@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Bgr2rgba(global::System.IntPtr __instance, global::System.IntPtr bgr_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgb2bgra@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Rgb2bgra(global::System.IntPtr __instance, global::System.IntPtr rgb_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?bgr2bgra@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Bgr2bgra(global::System.IntPtr __instance, global::System.IntPtr bgr_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgba2rgb@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Rgba2rgb(global::System.IntPtr __instance, global::System.IntPtr rgba_source, global::System.IntPtr rgb_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?rgba2bgr@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Rgba2bgr(global::System.IntPtr __instance, global::System.IntPtr rgba_source, global::System.IntPtr bgr_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?bgra2rgb@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Bgra2rgb(global::System.IntPtr __instance, global::System.IntPtr bgra_source, global::System.IntPtr rgb_dest, uint width, uint height, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?bgra2bgr@spoutCopy@@QEAAXPEAX0II_N@Z")]
            internal static extern void Bgra2bgr(global::System.IntPtr __instance, global::System.IntPtr bgra_source, global::System.IntPtr bgr_dest, uint width, uint height, bool bInvert);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutCopy> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutCopy>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutCopy __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutCopy(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutCopy __CreateInstance(global::Spout.Interop.SpoutCopy.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutCopy(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutCopy.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutCopy.__Internal));
            *(global::Spout.Interop.SpoutCopy.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutCopy(global::Spout.Interop.SpoutCopy.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutCopy(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutCopy()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutCopy.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutCopy(global::Spout.Interop.SpoutCopy _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutCopy.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutCopy.__Internal*)__Instance) = *((global::Spout.Interop.SpoutCopy.__Internal*)_0.__Instance);
        }

        ~SpoutCopy()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutCopy __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public void CopyPixels(byte* src, byte* dst, uint width, uint height, uint glFormat, bool bInvert)
        {
            __Internal.CopyPixels(__Instance, src, dst, width, height, glFormat, bInvert);
        }

        public bool FlipBuffer(byte* src, byte* dst, uint width, uint height, uint glFormat)
        {
            var __ret = __Internal.FlipBuffer(__Instance, src, dst, width, height, glFormat);
            return __ret;
        }

        public void MemcpySse2(global::System.IntPtr dst, global::System.IntPtr src, ulong size)
        {
            __Internal.MemcpySse2(__Instance, dst, src, size);
        }

        public void Rgba2bgra(global::System.IntPtr rgba_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Rgba2bgra(__Instance, rgba_source, bgra_dest, width, height, bInvert);
        }

        public void Bgra2rgba(global::System.IntPtr bgra_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Bgra2rgba(__Instance, bgra_source, rgba_dest, width, height, bInvert);
        }

        public void RgbaBgra(global::System.IntPtr rgba_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert)
        {
            __Internal.RgbaBgra(__Instance, rgba_source, bgra_dest, width, height, bInvert);
        }

        public void RgbaBgraSse2(global::System.IntPtr rgba_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert)
        {
            __Internal.RgbaBgraSse2(__Instance, rgba_source, rgba_dest, width, height, bInvert);
        }

        public void RgbaBgraSsse3(global::System.IntPtr rgba_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert)
        {
            __Internal.RgbaBgraSsse3(__Instance, rgba_source, rgba_dest, width, height, bInvert);
        }

        public void Rgb2rgba(global::System.IntPtr rgb_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Rgb2rgba(__Instance, rgb_source, rgba_dest, width, height, bInvert);
        }

        public void Bgr2rgba(global::System.IntPtr bgr_source, global::System.IntPtr rgba_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Bgr2rgba(__Instance, bgr_source, rgba_dest, width, height, bInvert);
        }

        public void Rgb2bgra(global::System.IntPtr rgb_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Rgb2bgra(__Instance, rgb_source, bgra_dest, width, height, bInvert);
        }

        public void Bgr2bgra(global::System.IntPtr bgr_source, global::System.IntPtr bgra_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Bgr2bgra(__Instance, bgr_source, bgra_dest, width, height, bInvert);
        }

        public void Rgba2rgb(global::System.IntPtr rgba_source, global::System.IntPtr rgb_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Rgba2rgb(__Instance, rgba_source, rgb_dest, width, height, bInvert);
        }

        public void Rgba2bgr(global::System.IntPtr rgba_source, global::System.IntPtr bgr_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Rgba2bgr(__Instance, rgba_source, bgr_dest, width, height, bInvert);
        }

        public void Bgra2rgb(global::System.IntPtr bgra_source, global::System.IntPtr rgb_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Bgra2rgb(__Instance, bgra_source, rgb_dest, width, height, bInvert);
        }

        public void Bgra2bgr(global::System.IntPtr bgra_source, global::System.IntPtr bgr_dest, uint width, uint height, bool bInvert)
        {
            __Internal.Bgra2bgr(__Instance, bgra_source, bgr_dest, width, height, bInvert);
        }
    }

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate global::System.IntPtr PFNWGLDXOPENDEVICENVPROC(global::System.IntPtr dxDevice);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLDXCLOSEDEVICENVPROC(global::System.IntPtr hDevice);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate global::System.IntPtr PFNWGLDXREGISTEROBJECTNVPROC(global::System.IntPtr hDevice, global::System.IntPtr dxObject, uint name, uint type, uint access);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLDXUNREGISTEROBJECTNVPROC(global::System.IntPtr hDevice, global::System.IntPtr hObject);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLDXSETRESOURCESHAREHANDLENVPROC(global::System.IntPtr dxResource, global::System.IntPtr shareHandle);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLDXLOCKOBJECTSNVPROC(global::System.IntPtr hDevice, int count, void** hObjects);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLDXUNLOCKOBJECTSNVPROC(global::System.IntPtr hDevice, int count, void** hObjects);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlBindFramebufferEXTPROC(uint target, uint framebuffer);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlBindRenderbufferEXTPROC(uint target, uint renderbuffer);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate uint GlCheckFramebufferStatusEXTPROC(uint target);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlDeleteFramebuffersEXTPROC(int n, uint* framebuffers);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlDeleteRenderBuffersEXTPROC(int n, uint* renderbuffers);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlFramebufferRenderbufferEXTPROC(uint target, uint attachment, uint renderbuffertarget, uint renderbuffer);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlFramebufferTexture1DEXTPROC(uint target, uint attachment, uint textarget, uint texture, int level);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlFramebufferTexture2DEXTPROC(uint target, uint attachment, uint textarget, uint texture, int level);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlFramebufferTexture3DEXTPROC(uint target, uint attachment, uint textarget, uint texture, int level, int zoffset);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlGenFramebuffersEXTPROC(int n, uint* framebuffers);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlGenRenderbuffersEXTPROC(int n, uint* renderbuffers);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlGenerateMipmapEXTPROC(uint target);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlGetFramebufferAttachmentParameterivEXTPROC(uint target, uint attachment, uint pname, int* @params);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlGetRenderbufferParameterivEXTPROC(uint target, uint pname, int* @params);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate byte GlIsFramebufferEXTPROC(uint framebuffer);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate byte GlIsRenderbufferEXTPROC(uint renderbuffer);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlRenderbufferStorageEXTPROC(uint target, uint internalformat, int width, int height);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlBlitFramebufferEXTPROC(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLSWAPINTERVALEXTPROC(int interval);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate int PFNWGLGETSWAPINTERVALEXTPROC();

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlGenBuffersPROC(int n, uint* buffers);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlDeleteBuffersPROC(int n, uint* buffers);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlBindBufferPROC(uint target, uint buffer);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlBufferDataPROC(uint target, long size, global::System.IntPtr data, uint usage);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate global::System.IntPtr GlMapBufferPROC(uint target, uint access);

    [SuppressUnmanagedCodeSecurity, UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public unsafe delegate void GlUnmapBufferPROC(uint target);

    public unsafe partial class SpoutGLextensions
    {
        public partial struct __Internal
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?InitializeGlew@@YA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool InitializeGlew();

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?loadGLextensions@@YAIXZ")]
            internal static extern uint LoadGLextensions();

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?loadInteropExtensions@@YA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadInteropExtensions();

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?loadFBOextensions@@YA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadFBOextensions();

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?loadBLITextension@@YA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadBLITextension();

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?loadSwapExtensions@@YA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadSwapExtensions();

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?loadPBOextensions@@YA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadPBOextensions();
        }

        public static bool InitializeGlew()
        {
            var __ret = __Internal.InitializeGlew();
            return __ret;
        }

        public static uint LoadGLextensions()
        {
            var __ret = __Internal.LoadGLextensions();
            return __ret;
        }

        public static bool LoadInteropExtensions()
        {
            var __ret = __Internal.LoadInteropExtensions();
            return __ret;
        }

        public static bool LoadFBOextensions()
        {
            var __ret = __Internal.LoadFBOextensions();
            return __ret;
        }

        public static bool LoadBLITextension()
        {
            var __ret = __Internal.LoadBLITextension();
            return __ret;
        }

        public static bool LoadSwapExtensions()
        {
            var __ret = __Internal.LoadSwapExtensions();
            return __ret;
        }

        public static bool LoadPBOextensions()
        {
            var __ret = __Internal.LoadPBOextensions();
            return __ret;
        }

        public static global::Spout.Interop.PFNWGLDXOPENDEVICENVPROC WglDXOpenDeviceNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXOpenDeviceNV@@3P6APEAXPEAX@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXOPENDEVICENVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXOPENDEVICENVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXOpenDeviceNV@@3P6APEAXPEAX@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLDXCLOSEDEVICENVPROC WglDXCloseDeviceNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXCloseDeviceNV@@3P6AHPEAX@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXCLOSEDEVICENVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXCLOSEDEVICENVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXCloseDeviceNV@@3P6AHPEAX@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLDXREGISTEROBJECTNVPROC WglDXRegisterObjectNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXRegisterObjectNV@@3P6APEAXPEAX0III@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXREGISTEROBJECTNVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXREGISTEROBJECTNVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXRegisterObjectNV@@3P6APEAXPEAX0III@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLDXUNREGISTEROBJECTNVPROC WglDXUnregisterObjectNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXUnregisterObjectNV@@3P6AHPEAX0@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXUNREGISTEROBJECTNVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXUNREGISTEROBJECTNVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXUnregisterObjectNV@@3P6AHPEAX0@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLDXSETRESOURCESHAREHANDLENVPROC WglDXSetResourceShareHandleNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXSetResourceShareHandleNV@@3P6AHPEAX0@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXSETRESOURCESHAREHANDLENVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXSETRESOURCESHAREHANDLENVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXSetResourceShareHandleNV@@3P6AHPEAX0@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLDXLOCKOBJECTSNVPROC WglDXLockObjectsNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXLockObjectsNV@@3P6AHPEAXHPEAPEAX@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXLOCKOBJECTSNVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXLOCKOBJECTSNVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXLockObjectsNV@@3P6AHPEAXHPEAPEAX@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLDXUNLOCKOBJECTSNVPROC WglDXUnlockObjectsNV
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXUnlockObjectsNV@@3P6AHPEAXHPEAPEAX@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLDXUNLOCKOBJECTSNVPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLDXUNLOCKOBJECTSNVPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglDXUnlockObjectsNV@@3P6AHPEAXHPEAPEAX@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlBindFramebufferEXTPROC GlBindFramebufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBindFramebufferEXT@@3P6AXII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlBindFramebufferEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlBindFramebufferEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBindFramebufferEXT@@3P6AXII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlBindRenderbufferEXTPROC GlBindRenderbufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBindRenderbufferEXT@@3P6AXII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlBindRenderbufferEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlBindRenderbufferEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBindRenderbufferEXT@@3P6AXII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlCheckFramebufferStatusEXTPROC GlCheckFramebufferStatusEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glCheckFramebufferStatusEXT@@3P6AII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlCheckFramebufferStatusEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlCheckFramebufferStatusEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glCheckFramebufferStatusEXT@@3P6AII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlDeleteFramebuffersEXTPROC GlDeleteFramebuffersEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glDeleteFramebuffersEXT@@3P6AXHPEBI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlDeleteFramebuffersEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlDeleteFramebuffersEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glDeleteFramebuffersEXT@@3P6AXHPEBI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlDeleteRenderBuffersEXTPROC GlDeleteRenderBuffersEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glDeleteRenderBuffersEXT@@3P6AXHPEBI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlDeleteRenderBuffersEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlDeleteRenderBuffersEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glDeleteRenderBuffersEXT@@3P6AXHPEBI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlFramebufferRenderbufferEXTPROC GlFramebufferRenderbufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferRenderbufferEXT@@3P6AXIIII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlFramebufferRenderbufferEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlFramebufferRenderbufferEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferRenderbufferEXT@@3P6AXIIII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlFramebufferTexture1DEXTPROC GlFramebufferTexture1DEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferTexture1DEXT@@3P6AXIIIIH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlFramebufferTexture1DEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlFramebufferTexture1DEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferTexture1DEXT@@3P6AXIIIIH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlFramebufferTexture2DEXTPROC GlFramebufferTexture2DEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferTexture2DEXT@@3P6AXIIIIH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlFramebufferTexture2DEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlFramebufferTexture2DEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferTexture2DEXT@@3P6AXIIIIH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlFramebufferTexture3DEXTPROC GlFramebufferTexture3DEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferTexture3DEXT@@3P6AXIIIIHH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlFramebufferTexture3DEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlFramebufferTexture3DEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glFramebufferTexture3DEXT@@3P6AXIIIIHH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlGenFramebuffersEXTPROC GlGenFramebuffersEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenFramebuffersEXT@@3P6AXHPEAI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlGenFramebuffersEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlGenFramebuffersEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenFramebuffersEXT@@3P6AXHPEAI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlGenRenderbuffersEXTPROC GlGenRenderbuffersEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenRenderbuffersEXT@@3P6AXHPEAI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlGenRenderbuffersEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlGenRenderbuffersEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenRenderbuffersEXT@@3P6AXHPEAI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlGenerateMipmapEXTPROC GlGenerateMipmapEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenerateMipmapEXT@@3P6AXI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlGenerateMipmapEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlGenerateMipmapEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenerateMipmapEXT@@3P6AXI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlGetFramebufferAttachmentParameterivEXTPROC GlGetFramebufferAttachmentParameterivEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGetFramebufferAttachmentParameterivEXT@@3P6AXIIIPEAH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlGetFramebufferAttachmentParameterivEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlGetFramebufferAttachmentParameterivEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGetFramebufferAttachmentParameterivEXT@@3P6AXIIIPEAH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlGetRenderbufferParameterivEXTPROC GlGetRenderbufferParameterivEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGetRenderbufferParameterivEXT@@3P6AXIIPEAH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlGetRenderbufferParameterivEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlGetRenderbufferParameterivEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGetRenderbufferParameterivEXT@@3P6AXIIPEAH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlIsFramebufferEXTPROC GlIsFramebufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glIsFramebufferEXT@@3P6AEI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlIsFramebufferEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlIsFramebufferEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glIsFramebufferEXT@@3P6AEI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlIsRenderbufferEXTPROC GlIsRenderbufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glIsRenderbufferEXT@@3P6AEI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlIsRenderbufferEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlIsRenderbufferEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glIsRenderbufferEXT@@3P6AEI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlRenderbufferStorageEXTPROC GlRenderbufferStorageEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glRenderbufferStorageEXT@@3P6AXIIHH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlRenderbufferStorageEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlRenderbufferStorageEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glRenderbufferStorageEXT@@3P6AXIIHH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlBlitFramebufferEXTPROC GlBlitFramebufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBlitFramebufferEXT@@3P6AXHHHHHHHHII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlBlitFramebufferEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlBlitFramebufferEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBlitFramebufferEXT@@3P6AXHHHHHHHHII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLSWAPINTERVALEXTPROC WglSwapIntervalEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglSwapIntervalEXT@@3P6AHH@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLSWAPINTERVALEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLSWAPINTERVALEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglSwapIntervalEXT@@3P6AHH@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.PFNWGLGETSWAPINTERVALEXTPROC WglGetSwapIntervalEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglGetSwapIntervalEXT@@3P6AHXZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.PFNWGLGETSWAPINTERVALEXTPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.PFNWGLGETSWAPINTERVALEXTPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?wglGetSwapIntervalEXT@@3P6AHXZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlGenBuffersPROC GlGenBuffersEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenBuffersEXT@@3P6AXHPEBI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlGenBuffersPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlGenBuffersPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glGenBuffersEXT@@3P6AXHPEBI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlDeleteBuffersPROC GlDeleteBuffersEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glDeleteBuffersEXT@@3P6AXHPEBI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlDeleteBuffersPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlDeleteBuffersPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glDeleteBuffersEXT@@3P6AXHPEBI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlBindBufferPROC GlBindBufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBindBufferEXT@@3P6AXII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlBindBufferPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlBindBufferPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBindBufferEXT@@3P6AXII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlBufferDataPROC GlBufferDataEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBufferDataEXT@@3P6AXI_JPEBXI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlBufferDataPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlBufferDataPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glBufferDataEXT@@3P6AXI_JPEBXI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlMapBufferPROC GlMapBufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glMapBufferEXT@@3P6APEAXII@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlMapBufferPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlMapBufferPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glMapBufferEXT@@3P6APEAXII@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        public static global::Spout.Interop.GlUnmapBufferPROC GlUnmapBufferEXT
        {
            get
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glUnmapBufferEXT@@3P6AXI@ZEA");
                var __ptr0 = *__ptr;
                return __ptr0 == IntPtr.Zero ? null : (global::Spout.Interop.GlUnmapBufferPROC)Marshal.GetDelegateForFunctionPointer(__ptr0, typeof(global::Spout.Interop.GlUnmapBufferPROC));
            }

            set
            {
                var __ptr = (global::System.IntPtr*)CppSharp.SymbolResolver.ResolveSymbol("Libraries/Spout.dll", "?glUnmapBufferEXT@@3P6AXI@ZEA");
                *__ptr = value == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(value);
            }
        }
    }

    public unsafe partial class SpoutGLDXinterop : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 664)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::Spout.Interop.SpoutSenderNames.__Internal senders;

            [FieldOffset(112)]
            internal global::Spout.Interop.SpoutDirectX.__Internal spoutdx;

            [FieldOffset(144)]
            internal global::Spout.Interop.SpoutCopy.__Internal spoutcopy;

            [FieldOffset(152)]
            internal global::Spout.Interop.SpoutMemoryShare.__Internal memoryshare;

            [FieldOffset(168)]
            internal byte m_bUseDX9;

            [FieldOffset(169)]
            internal byte m_bUseCPU;

            [FieldOffset(170)]
            internal byte m_bUseMemory;

            [FieldOffset(172)]
            internal global::D3DFORMAT DX9format;

            [FieldOffset(176)]
            internal global::DXGI_FORMAT DX11format;

            [FieldOffset(180)]
            internal uint m_glTexture;

            [FieldOffset(184)]
            internal uint m_fbo;

            [FieldOffset(192)]
            internal global::System.IntPtr m_pDevice;

            [FieldOffset(200)]
            internal global::System.IntPtr m_dxTexture;

            [FieldOffset(208)]
            internal global::System.IntPtr m_dxShareHandle;

            [FieldOffset(216)]
            internal global::System.IntPtr g_pd3dDevice;

            [FieldOffset(224)]
            internal global::System.IntPtr g_pSharedTexture;

            [FieldOffset(232)]
            internal byte m_bInitialized;

            [FieldOffset(233)]
            internal byte m_bExtensionsLoaded;

            [FieldOffset(236)]
            internal uint m_caps;

            [FieldOffset(240)]
            internal byte m_bFBOavailable;

            [FieldOffset(241)]
            internal byte m_bBLITavailable;

            [FieldOffset(242)]
            internal byte m_bPBOavailable;

            [FieldOffset(243)]
            internal byte m_bSWAPavailable;

            [FieldOffset(244)]
            internal byte m_bBGRAavailable;

            [FieldOffset(245)]
            internal byte m_bGLDXavailable;

            [FieldOffset(248)]
            internal global::System.IntPtr m_hWnd;

            [FieldOffset(256)]
            internal global::System.IntPtr m_hSharedMemory;

            [FieldOffset(264)]
            internal global::Spout.Interop.SharedTextureInfo.__Internal m_TextureInfo;

            [FieldOffset(544)]
            internal uint m_TexID;

            [FieldOffset(548)]
            internal uint m_TexWidth;

            [FieldOffset(552)]
            internal uint m_TexHeight;

            [FieldOffset(556)]
            internal fixed uint m_pbo[2];

            [FieldOffset(564)]
            internal int PboIndex;

            [FieldOffset(568)]
            internal int NextPboIndex;

            [FieldOffset(576)]
            internal global::System.IntPtr m_hdc;

            [FieldOffset(584)]
            internal global::System.IntPtr m_hwndButton;

            [FieldOffset(592)]
            internal global::System.IntPtr m_hRc;

            [FieldOffset(600)]
            internal global::System.IntPtr g_pImmediateContext;

            [FieldOffset(608)]
            internal global::D3D_DRIVER_TYPE g_driverType;

            [FieldOffset(612)]
            internal global::D3D_FEATURE_LEVEL g_featureLevel;

            [FieldOffset(616)]
            internal global::System.IntPtr g_pStagingTexture;

            [FieldOffset(624)]
            internal global::System.IntPtr m_pD3D;

            [FieldOffset(632)]
            internal global::System.IntPtr g_DX9surface;

            [FieldOffset(640)]
            internal global::System.IntPtr m_hInteropDevice;

            [FieldOffset(648)]
            internal global::System.IntPtr m_hInteropObject;

            [FieldOffset(656)]
            internal global::System.IntPtr m_hAccessMutex;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutGLDXinterop@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0spoutGLDXinterop@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1spoutGLDXinterop@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?LoadGLextensions@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadGLextensions(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CleanupInterop@spoutGLDXinterop@@QEAAX_N@Z")]
            internal static extern void CleanupInterop(global::System.IntPtr __instance, bool bExit);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?getSharedInfo@spoutGLDXinterop@@QEAA_NPEADPEAUSharedTextureInfo@@@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSharedInfo(global::System.IntPtr __instance, sbyte* sharedMemoryName, global::System.IntPtr info);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?setSharedInfo@spoutGLDXinterop@@QEAA_NPEADPEAUSharedTextureInfo@@@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetSharedInfo(global::System.IntPtr __instance, sbyte* sharedMemoryName, global::System.IntPtr info);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteTexture@spoutGLDXinterop@@QEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteTexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadTexture@spoutGLDXinterop@@QEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadTexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteTexturePixels@spoutGLDXinterop@@QEAA_NPEBEIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteTexturePixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadTexturePixels@spoutGLDXinterop@@QEAA_NPEAEIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadTexturePixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawSharedTexture@spoutGLDXinterop@@QEAA_NMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawSharedTexture(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToSharedTexture@spoutGLDXinterop@@QEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToSharedTexture(global::System.IntPtr __instance, uint TexID, uint TexTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?BindSharedTexture@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool BindSharedTexture(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UnloadTexturePixels@spoutGLDXinterop@@QEAA_NIIIIPEAEI_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UnloadTexturePixels(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, byte* data, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?LoadTexturePixels@spoutGLDXinterop@@QEAA_NIIIIPEBEI_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool LoadTexturePixels(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, byte* data, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetDX9@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetDX9(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetDX9@spoutGLDXinterop@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetDX9(global::System.IntPtr __instance, bool bDX9);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UseDX9@spoutGLDXinterop@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UseDX9(global::System.IntPtr __instance, bool bDX9);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetCPUmode@spoutGLDXinterop@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetCPUmode(global::System.IntPtr __instance, bool bCPU);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetMemoryShareMode@spoutGLDXinterop@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetMemoryShareMode(global::System.IntPtr __instance, bool bMem);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetShareMode@spoutGLDXinterop@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetShareMode(global::System.IntPtr __instance, int mode);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterName@spoutGLDXinterop@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterName(global::System.IntPtr __instance, int index, sbyte* adaptername, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetAdapter@spoutGLDXinterop@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetAdapter(global::System.IntPtr __instance, int index);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetHostPath@spoutGLDXinterop@@QEAA_NPEBDPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetHostPath(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, sbyte* hostpath, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateDX9interop@spoutGLDXinterop@@QEAA_NIIK_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateDX9interop(global::System.IntPtr __instance, uint width, uint height, uint dwFormat, bool bReceive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CleanupDX9@spoutGLDXinterop@@QEAAX_N@Z")]
            internal static extern void CleanupDX9(global::System.IntPtr __instance, bool bExit);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateDX11interop@spoutGLDXinterop@@QEAA_NIIK_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateDX11interop(global::System.IntPtr __instance, uint width, uint height, uint dwFormat, bool bReceive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?OpenDirectX11@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool OpenDirectX11(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CleanupDX11@spoutGLDXinterop@@QEAAX_N@Z")]
            internal static extern void CleanupDX11(global::System.IntPtr __instance, bool bExit);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CleanupDirectX@spoutGLDXinterop@@QEAAX_N@Z")]
            internal static extern void CleanupDirectX(global::System.IntPtr __instance, bool bExit);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?LinkGLDXtextures@spoutGLDXinterop@@QEAAPEAXPEAX00I@Z")]
            internal static extern global::System.IntPtr LinkGLDXtextures(global::System.IntPtr __instance, global::System.IntPtr pDXdevice, global::System.IntPtr pSharedTexture, global::System.IntPtr dxShareHandle, uint glTextureID);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?LockInteropObject@spoutGLDXinterop@@QEAAJPEAXPEAPEAX@Z")]
            internal static extern int LockInteropObject(global::System.IntPtr __instance, global::System.IntPtr hDevice, void** hObject);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UnlockInteropObject@spoutGLDXinterop@@QEAAJPEAXPEAPEAX@Z")]
            internal static extern int UnlockInteropObject(global::System.IntPtr __instance, global::System.IntPtr hDevice, void** hObject);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetVerticalSync@spoutGLDXinterop@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetVerticalSync(global::System.IntPtr __instance, bool bSync);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterInfo@spoutGLDXinterop@@QEAA_NPEAD0000HAEA_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterInfo(global::System.IntPtr __instance, sbyte* renderadapter, sbyte* renderdescription, sbyte* renderversion, sbyte* displaydescription, sbyte* displayversion, int maxsize, bool* bUseDX9);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CloseOpenGL@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CloseOpenGL(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CopyTexture@spoutGLDXinterop@@QEAA_NIIIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CopyTexture(global::System.IntPtr __instance, uint SourceID, uint SourceTarget, uint DestID, uint DestTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?InitTexture@spoutGLDXinterop@@QEAAXAEAIIII@Z")]
            internal static extern void InitTexture(global::System.IntPtr __instance, uint* texID, uint GLformat, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckOpenGLTexture@spoutGLDXinterop@@QEAAXAEAIIII00@Z")]
            internal static extern void CheckOpenGLTexture(global::System.IntPtr __instance, uint* texID, uint GLformat, uint newWidth, uint newHeight, uint* texWidth, uint* texHeight);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SaveOpenGLstate@spoutGLDXinterop@@QEAAXII_N@Z")]
            internal static extern void SaveOpenGLstate(global::System.IntPtr __instance, uint width, uint height, bool bFitWindow);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?RestoreOpenGLstate@spoutGLDXinterop@@QEAAXXZ")]
            internal static extern void RestoreOpenGLstate(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GLerror@spoutGLDXinterop@@QEAAXXZ")]
            internal static extern void GLerror(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?PrintFBOstatus@spoutGLDXinterop@@QEAAXI@Z")]
            internal static extern void PrintFBOstatus(global::System.IntPtr __instance, uint status);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?getSharedTextureInfo@spoutGLDXinterop@@IEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSharedTextureInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sharedMemoryName);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?setSharedTextureInfo@spoutGLDXinterop@@IEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetSharedTextureInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sharedMemoryName);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteGLDXtexture@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteGLDXtexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadGLDXtexture@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadGLDXtexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteGLDXpixels@spoutGLDXinterop@@IEAA_NPEBEIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteGLDXpixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadGLDXpixels@spoutGLDXinterop@@IEAA_NPEAEIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadGLDXpixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawGLDXtexture@spoutGLDXinterop@@IEAA_NMMM_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawGLDXtexture(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToGLDXtexture@spoutGLDXinterop@@IEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToGLDXtexture(global::System.IntPtr __instance, uint TexID, uint TexTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteDX11texture@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteDX11texture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadDX11texture@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadDX11texture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteDX11pixels@spoutGLDXinterop@@IEAA_NPEBEIII_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteDX11pixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadDX11pixels@spoutGLDXinterop@@IEAA_NPEAEIII_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadDX11pixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawDX11texture@spoutGLDXinterop@@IEAA_NMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawDX11texture(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToDX11texture@spoutGLDXinterop@@IEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToDX11texture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckStagingTexture@spoutGLDXinterop@@IEAA_NII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckStagingTexture(global::System.IntPtr __instance, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FlushWait@spoutGLDXinterop@@IEAAXXZ")]
            internal static extern void FlushWait(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteDX9texture@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteDX9texture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadDX9texture@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadDX9texture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteDX9pixels@spoutGLDXinterop@@IEAA_NPEBEIII_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteDX9pixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadDX9pixels@spoutGLDXinterop@@IEAA_NPEAEIII_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadDX9pixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawDX9texture@spoutGLDXinterop@@IEAA_NMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawDX9texture(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToDX9texture@spoutGLDXinterop@@IEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToDX9texture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckDX9surface@spoutGLDXinterop@@IEAA_NII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckDX9surface(global::System.IntPtr __instance, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteMemory@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteMemory(global::System.IntPtr __instance, uint TexID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadMemory@spoutGLDXinterop@@IEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadMemory(global::System.IntPtr __instance, uint TexID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WriteMemoryPixels@spoutGLDXinterop@@IEAA_NPEBEIII_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WriteMemoryPixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadMemoryPixels@spoutGLDXinterop@@IEAA_NPEAEIII_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadMemoryPixels(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawSharedMemory@spoutGLDXinterop@@IEAA_NMMM_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawSharedMemory(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToSharedMemory@spoutGLDXinterop@@IEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToSharedMemory(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?OpenDeviceKey@spoutGLDXinterop@@IEAA_NPEBDHPEAD1@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool OpenDeviceKey(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, int maxsize, sbyte* description, sbyte* version);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?trim@spoutGLDXinterop@@IEAAXPEAD@Z")]
            internal static extern void Trim(global::System.IntPtr __instance, sbyte* s);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UnBindSharedTexture@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UnBindSharedTexture(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?isDX9@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsDX9(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetCPUmode@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetCPUmode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetMemoryShareMode@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetMemoryShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetShareMode@spoutGLDXinterop@@QEAAHXZ")]
            internal static extern int GetShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?IsBGRAavailable@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsBGRAavailable(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?IsPBOavailable@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsPBOavailable(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetBufferMode@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetBufferMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetBufferMode@spoutGLDXinterop@@QEAAX_N@Z")]
            internal static extern void SetBufferMode(global::System.IntPtr __instance, bool bActive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetNumAdapters@spoutGLDXinterop@@QEAAHXZ")]
            internal static extern int GetNumAdapters(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapter@spoutGLDXinterop@@QEAAHXZ")]
            internal static extern int GetAdapter(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DX11available@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DX11available(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GLDXcompatible@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GLDXcompatible(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?isOptimus@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsOptimus(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetVerticalSync@spoutGLDXinterop@@QEAAHXZ")]
            internal static extern int GetVerticalSync(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSpoutVersion@spoutGLDXinterop@@QEAAKXZ")]
            internal static extern uint GetSpoutVersion(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?InitOpenGL@spoutGLDXinterop@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool InitOpenGL(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetGLtextureID@spoutGLDXinterop@@QEAAIXZ")]
            internal static extern uint GetGLtextureID(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutGLDXinterop> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutGLDXinterop>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutGLDXinterop __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutGLDXinterop(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutGLDXinterop __CreateInstance(global::Spout.Interop.SpoutGLDXinterop.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutGLDXinterop(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutGLDXinterop.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutGLDXinterop.__Internal));
            *(global::Spout.Interop.SpoutGLDXinterop.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutGLDXinterop(global::Spout.Interop.SpoutGLDXinterop.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutGLDXinterop(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutGLDXinterop()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutGLDXinterop.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutGLDXinterop(global::Spout.Interop.SpoutGLDXinterop _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutGLDXinterop.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance) = *((global::Spout.Interop.SpoutGLDXinterop.__Internal*)_0.__Instance);
        }

        ~SpoutGLDXinterop()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutGLDXinterop __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool LoadGLextensions()
        {
            var __ret = __Internal.LoadGLextensions(__Instance);
            return __ret;
        }

        public void CleanupInterop(bool bExit)
        {
            __Internal.CleanupInterop(__Instance, bExit);
        }

        public bool GetSharedInfo(sbyte* sharedMemoryName, global::Spout.Interop.SharedTextureInfo info)
        {
            var __arg1 = ReferenceEquals(info, null) ? global::System.IntPtr.Zero : info.__Instance;
            var __ret = __Internal.GetSharedInfo(__Instance, sharedMemoryName, __arg1);
            return __ret;
        }

        public bool SetSharedInfo(sbyte* sharedMemoryName, global::Spout.Interop.SharedTextureInfo info)
        {
            var __arg1 = ReferenceEquals(info, null) ? global::System.IntPtr.Zero : info.__Instance;
            var __ret = __Internal.SetSharedInfo(__Instance, sharedMemoryName, __arg1);
            return __ret;
        }

        public bool WriteTexture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteTexture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        public bool ReadTexture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadTexture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        public bool WriteTexturePixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteTexturePixels(__Instance, pixels, width, height, glFormat, bInvert, HostFBO);
            return __ret;
        }

        public bool ReadTexturePixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadTexturePixels(__Instance, pixels, width, height, glFormat, bInvert, HostFBO);
            return __ret;
        }

        public bool DrawSharedTexture(float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawSharedTexture(__Instance, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        public bool DrawToSharedTexture(uint TexID, uint TexTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToSharedTexture(__Instance, TexID, TexTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        public bool BindSharedTexture()
        {
            var __ret = __Internal.BindSharedTexture(__Instance);
            return __ret;
        }

        public bool UnloadTexturePixels(uint TextureID, uint TextureTarget, uint width, uint height, byte* data, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.UnloadTexturePixels(__Instance, TextureID, TextureTarget, width, height, data, glFormat, bInvert, HostFBO);
            return __ret;
        }

        public bool LoadTexturePixels(uint TextureID, uint TextureTarget, uint width, uint height, byte* data, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.LoadTexturePixels(__Instance, TextureID, TextureTarget, width, height, data, glFormat, bInvert);
            return __ret;
        }

        public bool GetDX9()
        {
            var __ret = __Internal.GetDX9(__Instance);
            return __ret;
        }

        public bool SetDX9(bool bDX9)
        {
            var __ret = __Internal.SetDX9(__Instance, bDX9);
            return __ret;
        }

        public bool UseDX9(bool bDX9)
        {
            var __ret = __Internal.UseDX9(__Instance, bDX9);
            return __ret;
        }

        public bool SetCPUmode(bool bCPU)
        {
            var __ret = __Internal.SetCPUmode(__Instance, bCPU);
            return __ret;
        }

        public bool SetMemoryShareMode(bool bMem)
        {
            var __ret = __Internal.SetMemoryShareMode(__Instance, bMem);
            return __ret;
        }

        public bool SetShareMode(int mode)
        {
            var __ret = __Internal.SetShareMode(__Instance, mode);
            return __ret;
        }

        public bool GetAdapterName(int index, sbyte* adaptername, int maxchars)
        {
            var __ret = __Internal.GetAdapterName(__Instance, index, adaptername, maxchars);
            return __ret;
        }

        public bool SetAdapter(int index)
        {
            var __ret = __Internal.SetAdapter(__Instance, index);
            return __ret;
        }

        public bool GetHostPath(string sendername, sbyte* hostpath, int maxchars)
        {
            var __ret = __Internal.GetHostPath(__Instance, sendername, hostpath, maxchars);
            return __ret;
        }

        public bool CreateDX9interop(uint width, uint height, uint dwFormat, bool bReceive)
        {
            var __ret = __Internal.CreateDX9interop(__Instance, width, height, dwFormat, bReceive);
            return __ret;
        }

        public void CleanupDX9(bool bExit)
        {
            __Internal.CleanupDX9(__Instance, bExit);
        }

        public bool CreateDX11interop(uint width, uint height, uint dwFormat, bool bReceive)
        {
            var __ret = __Internal.CreateDX11interop(__Instance, width, height, dwFormat, bReceive);
            return __ret;
        }

        public bool OpenDirectX11()
        {
            var __ret = __Internal.OpenDirectX11(__Instance);
            return __ret;
        }

        public void CleanupDX11(bool bExit)
        {
            __Internal.CleanupDX11(__Instance, bExit);
        }

        public void CleanupDirectX(bool bExit)
        {
            __Internal.CleanupDirectX(__Instance, bExit);
        }

        public global::System.IntPtr LinkGLDXtextures(global::System.IntPtr pDXdevice, global::System.IntPtr pSharedTexture, global::System.IntPtr dxShareHandle, uint glTextureID)
        {
            var __ret = __Internal.LinkGLDXtextures(__Instance, pDXdevice, pSharedTexture, dxShareHandle, glTextureID);
            return __ret;
        }

        public int LockInteropObject(global::System.IntPtr hDevice, void** hObject)
        {
            var __ret = __Internal.LockInteropObject(__Instance, hDevice, hObject);
            return __ret;
        }

        public int UnlockInteropObject(global::System.IntPtr hDevice, void** hObject)
        {
            var __ret = __Internal.UnlockInteropObject(__Instance, hDevice, hObject);
            return __ret;
        }

        public bool SetVerticalSync(bool bSync)
        {
            var __ret = __Internal.SetVerticalSync(__Instance, bSync);
            return __ret;
        }

        public bool GetAdapterInfo(sbyte* renderadapter, sbyte* renderdescription, sbyte* renderversion, sbyte* displaydescription, sbyte* displayversion, int maxsize, ref bool bUseDX9)
        {
            fixed (bool* __bUseDX96 = &bUseDX9)
            {
                var __arg6 = __bUseDX96;
                var __ret = __Internal.GetAdapterInfo(__Instance, renderadapter, renderdescription, renderversion, displaydescription, displayversion, maxsize, __arg6);
                return __ret;
            }
        }

        public bool CloseOpenGL()
        {
            var __ret = __Internal.CloseOpenGL(__Instance);
            return __ret;
        }

        public bool CopyTexture(uint SourceID, uint SourceTarget, uint DestID, uint DestTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.CopyTexture(__Instance, SourceID, SourceTarget, DestID, DestTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        public void InitTexture(ref uint texID, uint GLformat, uint width, uint height)
        {
            fixed (uint* __texID0 = &texID)
            {
                var __arg0 = __texID0;
                __Internal.InitTexture(__Instance, __arg0, GLformat, width, height);
            }
        }

        public void CheckOpenGLTexture(ref uint texID, uint GLformat, uint newWidth, uint newHeight, ref uint texWidth, ref uint texHeight)
        {
            fixed (uint* __texID0 = &texID)
            {
                var __arg0 = __texID0;
                fixed (uint* __texWidth4 = &texWidth)
                {
                    var __arg4 = __texWidth4;
                    fixed (uint* __texHeight5 = &texHeight)
                    {
                        var __arg5 = __texHeight5;
                        __Internal.CheckOpenGLTexture(__Instance, __arg0, GLformat, newWidth, newHeight, __arg4, __arg5);
                    }
                }
            }
        }

        public void SaveOpenGLstate(uint width, uint height, bool bFitWindow)
        {
            __Internal.SaveOpenGLstate(__Instance, width, height, bFitWindow);
        }

        public void RestoreOpenGLstate()
        {
            __Internal.RestoreOpenGLstate(__Instance);
        }

        public void GLerror()
        {
            __Internal.GLerror(__Instance);
        }

        public void PrintFBOstatus(uint status)
        {
            __Internal.PrintFBOstatus(__Instance, status);
        }

        protected bool GetSharedTextureInfo(string sharedMemoryName)
        {
            var __ret = __Internal.GetSharedTextureInfo(__Instance, sharedMemoryName);
            return __ret;
        }

        protected bool SetSharedTextureInfo(string sharedMemoryName)
        {
            var __ret = __Internal.SetSharedTextureInfo(__Instance, sharedMemoryName);
            return __ret;
        }

        protected bool WriteGLDXtexture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteGLDXtexture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool ReadGLDXtexture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadGLDXtexture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool WriteGLDXpixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteGLDXpixels(__Instance, pixels, width, height, glFormat, bInvert, HostFBO);
            return __ret;
        }

        protected bool ReadGLDXpixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadGLDXpixels(__Instance, pixels, width, height, glFormat, bInvert, HostFBO);
            return __ret;
        }

        protected bool DrawGLDXtexture(float max_x, float max_y, float aspect, bool bInvert)
        {
            var __ret = __Internal.DrawGLDXtexture(__Instance, max_x, max_y, aspect, bInvert);
            return __ret;
        }

        protected bool DrawToGLDXtexture(uint TexID, uint TexTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToGLDXtexture(__Instance, TexID, TexTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        protected bool WriteDX11texture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteDX11texture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool ReadDX11texture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadDX11texture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool WriteDX11pixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.WriteDX11pixels(__Instance, pixels, width, height, glFormat, bInvert);
            return __ret;
        }

        protected bool ReadDX11pixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.ReadDX11pixels(__Instance, pixels, width, height, glFormat, bInvert);
            return __ret;
        }

        protected bool DrawDX11texture(float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawDX11texture(__Instance, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        protected bool DrawToDX11texture(uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToDX11texture(__Instance, TextureID, TextureTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        protected bool CheckStagingTexture(uint width, uint height)
        {
            var __ret = __Internal.CheckStagingTexture(__Instance, width, height);
            return __ret;
        }

        protected void FlushWait()
        {
            __Internal.FlushWait(__Instance);
        }

        protected bool WriteDX9texture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteDX9texture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool ReadDX9texture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadDX9texture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool WriteDX9pixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.WriteDX9pixels(__Instance, pixels, width, height, glFormat, bInvert);
            return __ret;
        }

        protected bool ReadDX9pixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.ReadDX9pixels(__Instance, pixels, width, height, glFormat, bInvert);
            return __ret;
        }

        protected bool DrawDX9texture(float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawDX9texture(__Instance, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        protected bool DrawToDX9texture(uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToDX9texture(__Instance, TextureID, TextureTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        protected bool CheckDX9surface(uint width, uint height)
        {
            var __ret = __Internal.CheckDX9surface(__Instance, width, height);
            return __ret;
        }

        protected bool WriteMemory(uint TexID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.WriteMemory(__Instance, TexID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool ReadMemory(uint TexID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.ReadMemory(__Instance, TexID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        protected bool WriteMemoryPixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.WriteMemoryPixels(__Instance, pixels, width, height, glFormat, bInvert);
            return __ret;
        }

        protected bool ReadMemoryPixels(byte* pixels, uint width, uint height, uint glFormat, bool bInvert)
        {
            var __ret = __Internal.ReadMemoryPixels(__Instance, pixels, width, height, glFormat, bInvert);
            return __ret;
        }

        protected bool DrawSharedMemory(float max_x, float max_y, float aspect, bool bInvert)
        {
            var __ret = __Internal.DrawSharedMemory(__Instance, max_x, max_y, aspect, bInvert);
            return __ret;
        }

        protected bool DrawToSharedMemory(uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToSharedMemory(__Instance, TextureID, TextureTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        protected bool OpenDeviceKey(string key, int maxsize, sbyte* description, sbyte* version)
        {
            var __ret = __Internal.OpenDeviceKey(__Instance, key, maxsize, description, version);
            return __ret;
        }

        protected void Trim(sbyte* s)
        {
            __Internal.Trim(__Instance, s);
        }

        public global::Spout.Interop.SpoutSenderNames Senders
        {
            get
            {
                return global::Spout.Interop.SpoutSenderNames.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->senders));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->senders = *(global::Spout.Interop.SpoutSenderNames.__Internal*)value.__Instance;
            }
        }

        public global::Spout.Interop.SpoutDirectX Spoutdx
        {
            get
            {
                return global::Spout.Interop.SpoutDirectX.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->spoutdx));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->spoutdx = *(global::Spout.Interop.SpoutDirectX.__Internal*)value.__Instance;
            }
        }

        public global::Spout.Interop.SpoutCopy Spoutcopy
        {
            get
            {
                return global::Spout.Interop.SpoutCopy.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->spoutcopy));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->spoutcopy = *(global::Spout.Interop.SpoutCopy.__Internal*)value.__Instance;
            }
        }

        public global::Spout.Interop.SpoutMemoryShare Memoryshare
        {
            get
            {
                return global::Spout.Interop.SpoutMemoryShare.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->memoryshare));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->memoryshare = *(global::Spout.Interop.SpoutMemoryShare.__Internal*)value.__Instance;
            }
        }

        public bool MBUseDX9
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bUseDX9 != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bUseDX9 = (byte)(value ? 1 : 0);
            }
        }

        public bool MBUseCPU
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bUseCPU != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bUseCPU = (byte)(value ? 1 : 0);
            }
        }

        public bool MBUseMemory
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bUseMemory != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bUseMemory = (byte)(value ? 1 : 0);
            }
        }

        public uint MGlTexture
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_glTexture;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_glTexture = value;
            }
        }

        public uint MFbo
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_fbo;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_fbo = value;
            }
        }

        public global::System.IntPtr MDxShareHandle
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_dxShareHandle;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_dxShareHandle = (global::System.IntPtr)value;
            }
        }

        protected bool MBInitialized
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bInitialized != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bInitialized = (byte)(value ? 1 : 0);
            }
        }

        protected bool MBExtensionsLoaded
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bExtensionsLoaded != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bExtensionsLoaded = (byte)(value ? 1 : 0);
            }
        }

        protected uint MCaps
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_caps;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_caps = value;
            }
        }

        protected bool MBFBOavailable
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bFBOavailable != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bFBOavailable = (byte)(value ? 1 : 0);
            }
        }

        protected bool MBBLITavailable
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bBLITavailable != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bBLITavailable = (byte)(value ? 1 : 0);
            }
        }

        protected bool MBPBOavailable
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bPBOavailable != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bPBOavailable = (byte)(value ? 1 : 0);
            }
        }

        protected bool MBSWAPavailable
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bSWAPavailable != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bSWAPavailable = (byte)(value ? 1 : 0);
            }
        }

        protected bool MBBGRAavailable
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bBGRAavailable != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bBGRAavailable = (byte)(value ? 1 : 0);
            }
        }

        protected bool MBGLDXavailable
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bGLDXavailable != 0;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_bGLDXavailable = (byte)(value ? 1 : 0);
            }
        }

        protected global::System.IntPtr MHSharedMemory
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hSharedMemory;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hSharedMemory = (global::System.IntPtr)value;
            }
        }

        protected global::Spout.Interop.SharedTextureInfo MTextureInfo
        {
            get
            {
                return global::Spout.Interop.SharedTextureInfo.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TextureInfo));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TextureInfo = *(global::Spout.Interop.SharedTextureInfo.__Internal*)value.__Instance;
            }
        }

        protected uint MTexID
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TexID;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TexID = value;
            }
        }

        protected uint MTexWidth
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TexWidth;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TexWidth = value;
            }
        }

        protected uint MTexHeight
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TexHeight;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_TexHeight = value;
            }
        }

        protected uint[] MPbo
        {
            get
            {
                uint[] __value = null;
                if (((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_pbo != null)
                {
                    __value = new uint[2];
                    for (int i = 0; i < 2; i++)
                        __value[i] = ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_pbo[i];
                }
                return __value;
            }

            set
            {
                if (value != null)
                {
                    for (int i = 0; i < 2; i++)
                        ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_pbo[i] = value[i];
                }
            }
        }

        protected int PboIndex
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->PboIndex;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->PboIndex = value;
            }
        }

        protected int NextPboIndex
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->NextPboIndex;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->NextPboIndex = value;
            }
        }

        protected global::System.IntPtr MHInteropDevice
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hInteropDevice;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hInteropDevice = (global::System.IntPtr)value;
            }
        }

        protected global::System.IntPtr MHInteropObject
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hInteropObject;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hInteropObject = (global::System.IntPtr)value;
            }
        }

        protected global::System.IntPtr MHAccessMutex
        {
            get
            {
                return ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hAccessMutex;
            }

            set
            {
                ((global::Spout.Interop.SpoutGLDXinterop.__Internal*)__Instance)->m_hAccessMutex = (global::System.IntPtr)value;
            }
        }

        public bool UnBindSharedTexture
        {
            get
            {
                var __ret = __Internal.UnBindSharedTexture(__Instance);
                return __ret;
            }
        }

        public bool DX9
        {
            get
            {
                var __ret = __Internal.IsDX9(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetDX9(__Instance, value);
            }
        }

        public bool CPUmode
        {
            get
            {
                var __ret = __Internal.GetCPUmode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetCPUmode(__Instance, value);
            }
        }

        public bool MemoryShareMode
        {
            get
            {
                var __ret = __Internal.GetMemoryShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetMemoryShareMode(__Instance, value);
            }
        }

        public int ShareMode
        {
            get
            {
                var __ret = __Internal.GetShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetShareMode(__Instance, value);
            }
        }

        public bool IsBGRAavailable
        {
            get
            {
                var __ret = __Internal.IsBGRAavailable(__Instance);
                return __ret;
            }
        }

        public bool IsPBOavailable
        {
            get
            {
                var __ret = __Internal.IsPBOavailable(__Instance);
                return __ret;
            }
        }

        public bool BufferMode
        {
            get
            {
                var __ret = __Internal.GetBufferMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetBufferMode(__Instance, value);
            }
        }

        public int NumAdapters
        {
            get
            {
                var __ret = __Internal.GetNumAdapters(__Instance);
                return __ret;
            }
        }

        public int Adapter
        {
            get
            {
                var __ret = __Internal.GetAdapter(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetAdapter(__Instance, value);
            }
        }

        public bool DX11available
        {
            get
            {
                var __ret = __Internal.DX11available(__Instance);
                return __ret;
            }
        }

        public bool GLDXcompatible
        {
            get
            {
                var __ret = __Internal.GLDXcompatible(__Instance);
                return __ret;
            }
        }

        public bool IsOptimus
        {
            get
            {
                var __ret = __Internal.IsOptimus(__Instance);
                return __ret;
            }
        }

        public int VerticalSync
        {
            get
            {
                var __ret = __Internal.GetVerticalSync(__Instance);
                return __ret;
            }
        }

        public uint SpoutVersion
        {
            get
            {
                var __ret = __Internal.GetSpoutVersion(__Instance);
                return __ret;
            }
        }

        public bool InitOpenGL
        {
            get
            {
                var __ret = __Internal.InitOpenGL(__Instance);
                return __ret;
            }
        }

        public uint GLtextureID
        {
            get
            {
                var __ret = __Internal.GetGLtextureID(__Instance);
                return __ret;
            }
        }
    }

    public unsafe partial class Spout : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 1336)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::Spout.Interop.SpoutGLDXinterop.__Internal interop;

            [FieldOffset(664)]
            internal fixed sbyte g_SharedMemoryName[256];

            [FieldOffset(920)]
            internal fixed sbyte UserSenderName[256];

            [FieldOffset(1176)]
            internal uint g_Width;

            [FieldOffset(1180)]
            internal uint g_Height;

            [FieldOffset(1184)]
            internal global::System.IntPtr g_ShareHandle;

            [FieldOffset(1192)]
            internal uint g_Format;

            [FieldOffset(1196)]
            internal uint g_TexID;

            [FieldOffset(1200)]
            internal global::System.IntPtr g_hWnd;

            [FieldOffset(1208)]
            internal byte bGLDXcompatible;

            [FieldOffset(1209)]
            internal byte bMemoryShareInitOK;

            [FieldOffset(1210)]
            internal byte bDxInitOK;

            [FieldOffset(1211)]
            internal byte bUseCPU;

            [FieldOffset(1212)]
            internal byte bMemory;

            [FieldOffset(1213)]
            internal byte bInitialized;

            [FieldOffset(1214)]
            internal byte bIsSending;

            [FieldOffset(1215)]
            internal byte bIsReceiving;

            [FieldOffset(1216)]
            internal byte bChangeRequested;

            [FieldOffset(1217)]
            internal byte bSpoutPanelOpened;

            [FieldOffset(1218)]
            internal byte bSpoutPanelActive;

            [FieldOffset(1219)]
            internal byte bUseActive;

            [FieldOffset(1224)]
            internal global::SHELLEXECUTEINFOA.__Internal m_ShExecInfo;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0Spout@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0Spout@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1Spout@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateSender@Spout@@QEAA_NPEBDIIK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername, uint width, uint height, uint dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UpdateSender@Spout@@QEAA_NPEBDII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UpdateSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseSender@Spout@@QEAAXK@Z")]
            internal static extern void ReleaseSender(global::System.IntPtr __instance, uint dwMsec);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateReceiver@Spout@@QEAA_NPEADAEAI1_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateReceiver(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, bool bUseActive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseReceiver@Spout@@QEAAXXZ")]
            internal static extern void ReleaseReceiver(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckReceiver@Spout@@QEAA_NPEADAEAI1AEA_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckReceiver(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, bool* bConnected);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetImageSize@Spout@@QEAA_NPEADAEAI1AEA_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetImageSize(global::System.IntPtr __instance, sbyte* sendername, uint* width, uint* height, bool* mMemoryMode);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SendTexture@Spout@@QEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SendTexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SendImage@Spout@@QEAA_NPEBEIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SendImage(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReceiveTexture@Spout@@QEAA_NPEADAEAI1II_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReceiveTexture(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, uint TextureID, uint TextureTarget, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReceiveImage@Spout@@QEAA_NPEADAEAI1PEAEI_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReceiveImage(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, byte* pixels, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawSharedTexture@Spout@@QEAA_NMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawSharedTexture(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToSharedTexture@Spout@@QEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToSharedTexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?BindSharedTexture@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool BindSharedTexture(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderName@Spout@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderName(global::System.IntPtr __instance, int index, sbyte* sendername, int MaxSize);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderInfo@Spout@@QEAA_NPEBDAEAI1AEAPEAXAEAK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, uint* width, uint* height, void** dxShareHandle, uint* dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetActiveSender@Spout@@QEAA_NPEAD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetActiveSender(global::System.IntPtr __instance, sbyte* Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetActiveSender@Spout@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetActiveSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetDX9@Spout@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetDX9(global::System.IntPtr __instance, bool bDX9);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetMemoryShareMode@Spout@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetMemoryShareMode(global::System.IntPtr __instance, bool bMem);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetCPUmode@Spout@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetCPUmode(global::System.IntPtr __instance, bool bCPU);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetShareMode@Spout@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetShareMode(global::System.IntPtr __instance, int mode);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSpoutSenderName@Spout@@QEAA_NPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSpoutSenderName(global::System.IntPtr __instance, sbyte* sendername, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterName@Spout@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterName(global::System.IntPtr __instance, int index, sbyte* adaptername, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetAdapter@Spout@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetAdapter(global::System.IntPtr __instance, int index);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetHostPath@Spout@@QEAA_NPEBDPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetHostPath(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, sbyte* hostpath, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetVerticalSync@Spout@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetVerticalSync(global::System.IntPtr __instance, bool bSync);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SelectSenderPanel@Spout@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SelectSenderPanel(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckSpoutPanel@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckSpoutPanel(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?OpenSpout@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool OpenSpout(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?WritePathToRegistry@Spout@@QEAA_NPEBD00@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool WritePathToRegistry(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string filepath, [MarshalAs(UnmanagedType.LPUTF8Str)] string subkey, [MarshalAs(UnmanagedType.LPUTF8Str)] string valuename);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReadPathFromRegistry@Spout@@QEAA_NPEADPEBD1@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReadPathFromRegistry(global::System.IntPtr __instance, sbyte* filepath, [MarshalAs(UnmanagedType.LPUTF8Str)] string subkey, [MarshalAs(UnmanagedType.LPUTF8Str)] string valuename);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?RemovePathFromRegistry@Spout@@QEAA_NPEBD0@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool RemovePathFromRegistry(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string subkey, [MarshalAs(UnmanagedType.LPUTF8Str)] string valuename);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UseAccessLocks@Spout@@QEAAX_N@Z")]
            internal static extern void UseAccessLocks(global::System.IntPtr __instance, bool bUseLocks);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SpoutCleanUp@Spout@@QEAAX_N@Z")]
            internal static extern void SpoutCleanUp(global::System.IntPtr __instance, bool bExit);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CleanSenders@Spout@@QEAAXXZ")]
            internal static extern void CleanSenders(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?OpenReceiver@Spout@@IEAA_NPEADAEAI1@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool OpenReceiver(global::System.IntPtr __instance, sbyte* name, uint* width, uint* height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?InitMemoryShare@Spout@@IEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool InitMemoryShare(global::System.IntPtr __instance, bool bReceiver);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?FindFileVersion@Spout@@IEAA_NPEBDAEAK1@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool FindFileVersion(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string filepath, uint* versMS, uint* versLS);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UnBindSharedTexture@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UnBindSharedTexture(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderCount@Spout@@QEAAHXZ")]
            internal static extern int GetSenderCount(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetDX9@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetDX9(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetMemoryShareMode@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetMemoryShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetCPUmode@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetCPUmode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetShareMode@Spout@@QEAAHXZ")]
            internal static extern int GetShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetMaxSenders@Spout@@QEAAHXZ")]
            internal static extern int GetMaxSenders(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetMaxSenders@Spout@@QEAAXH@Z")]
            internal static extern void SetMaxSenders(global::System.IntPtr __instance, int maxSenders);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?IsSpoutInitialized@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsSpoutInitialized(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?IsBGRAavailable@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsBGRAavailable(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?IsPBOavailable@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool IsPBOavailable(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetBufferMode@Spout@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetBufferMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetBufferMode@Spout@@QEAAX_N@Z")]
            internal static extern void SetBufferMode(global::System.IntPtr __instance, bool bActive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetNumAdapters@Spout@@QEAAHXZ")]
            internal static extern int GetNumAdapters(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapter@Spout@@QEAAHXZ")]
            internal static extern int GetAdapter(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetVerticalSync@Spout@@QEAAHXZ")]
            internal static extern int GetVerticalSync(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReportMemory@Spout@@QEAAHXZ")]
            internal static extern int ReportMemory(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GLDXcompatible@Spout@@IEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GLDXcompatible(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseMemoryShare@Spout@@IEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReleaseMemoryShare(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.Spout> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.Spout>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.Spout __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.Spout(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.Spout __CreateInstance(global::Spout.Interop.Spout.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.Spout(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.Spout.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.Spout.__Internal));
            *(global::Spout.Interop.Spout.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private Spout(global::Spout.Interop.Spout.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected Spout(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public Spout()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.Spout.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public Spout(global::Spout.Interop.Spout _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.Spout.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.Spout.__Internal*)__Instance) = *((global::Spout.Interop.Spout.__Internal*)_0.__Instance);
        }

        ~Spout()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.Spout __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool CreateSender(string Sendername, uint width, uint height, uint dwFormat)
        {
            var __ret = __Internal.CreateSender(__Instance, Sendername, width, height, dwFormat);
            return __ret;
        }

        public bool UpdateSender(string Sendername, uint width, uint height)
        {
            var __ret = __Internal.UpdateSender(__Instance, Sendername, width, height);
            return __ret;
        }

        public void ReleaseSender(uint dwMsec)
        {
            __Internal.ReleaseSender(__Instance, dwMsec);
        }

        public bool CreateReceiver(sbyte* Sendername, ref uint width, ref uint height, bool bUseActive)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.CreateReceiver(__Instance, Sendername, __arg1, __arg2, bUseActive);
                    return __ret;
                }
            }
        }

        public void ReleaseReceiver()
        {
            __Internal.ReleaseReceiver(__Instance);
        }

        public bool CheckReceiver(sbyte* Sendername, ref uint width, ref uint height, ref bool bConnected)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (bool* __bConnected3 = &bConnected)
                    {
                        var __arg3 = __bConnected3;
                        var __ret = __Internal.CheckReceiver(__Instance, Sendername, __arg1, __arg2, __arg3);
                        return __ret;
                    }
                }
            }
        }

        public bool GetImageSize(sbyte* sendername, ref uint width, ref uint height, ref bool mMemoryMode)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (bool* __mMemoryMode3 = &mMemoryMode)
                    {
                        var __arg3 = __mMemoryMode3;
                        var __ret = __Internal.GetImageSize(__Instance, sendername, __arg1, __arg2, __arg3);
                        return __ret;
                    }
                }
            }
        }

        public bool SendTexture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.SendTexture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        public bool SendImage(byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.SendImage(__Instance, pixels, width, height, glFormat, bInvert, HostFBO);
            return __ret;
        }

        public bool ReceiveTexture(sbyte* Sendername, ref uint width, ref uint height, uint TextureID, uint TextureTarget, bool bInvert, uint HostFBO)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.ReceiveTexture(__Instance, Sendername, __arg1, __arg2, TextureID, TextureTarget, bInvert, HostFBO);
                    return __ret;
                }
            }
        }

        public bool ReceiveImage(sbyte* Sendername, ref uint width, ref uint height, byte* pixels, uint glFormat, bool bInvert, uint HostFBO)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.ReceiveImage(__Instance, Sendername, __arg1, __arg2, pixels, glFormat, bInvert, HostFBO);
                    return __ret;
                }
            }
        }

        public bool DrawSharedTexture(float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawSharedTexture(__Instance, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        public bool DrawToSharedTexture(uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToSharedTexture(__Instance, TextureID, TextureTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        public bool BindSharedTexture()
        {
            var __ret = __Internal.BindSharedTexture(__Instance);
            return __ret;
        }

        public bool GetSenderName(int index, sbyte* sendername, int MaxSize)
        {
            var __ret = __Internal.GetSenderName(__Instance, index, sendername, MaxSize);
            return __ret;
        }

        public bool GetSenderInfo(string sendername, ref uint width, ref uint height, void** dxShareHandle, ref uint dwFormat)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (uint* __dwFormat4 = &dwFormat)
                    {
                        var __arg4 = __dwFormat4;
                        var __ret = __Internal.GetSenderInfo(__Instance, sendername, __arg1, __arg2, dxShareHandle, __arg4);
                        return __ret;
                    }
                }
            }
        }

        public bool GetActiveSender(sbyte* Sendername)
        {
            var __ret = __Internal.GetActiveSender(__Instance, Sendername);
            return __ret;
        }

        public bool SetActiveSender(string Sendername)
        {
            var __ret = __Internal.SetActiveSender(__Instance, Sendername);
            return __ret;
        }

        public bool SetDX9(bool bDX9)
        {
            var __ret = __Internal.SetDX9(__Instance, bDX9);
            return __ret;
        }

        public bool SetMemoryShareMode(bool bMem)
        {
            var __ret = __Internal.SetMemoryShareMode(__Instance, bMem);
            return __ret;
        }

        public bool SetCPUmode(bool bCPU)
        {
            var __ret = __Internal.SetCPUmode(__Instance, bCPU);
            return __ret;
        }

        public bool SetShareMode(int mode)
        {
            var __ret = __Internal.SetShareMode(__Instance, mode);
            return __ret;
        }

        public bool GetSpoutSenderName(sbyte* sendername, int maxchars)
        {
            var __ret = __Internal.GetSpoutSenderName(__Instance, sendername, maxchars);
            return __ret;
        }

        public bool GetAdapterName(int index, sbyte* adaptername, int maxchars)
        {
            var __ret = __Internal.GetAdapterName(__Instance, index, adaptername, maxchars);
            return __ret;
        }

        public bool SetAdapter(int index)
        {
            var __ret = __Internal.SetAdapter(__Instance, index);
            return __ret;
        }

        public bool GetHostPath(string sendername, sbyte* hostpath, int maxchars)
        {
            var __ret = __Internal.GetHostPath(__Instance, sendername, hostpath, maxchars);
            return __ret;
        }

        public bool SetVerticalSync(bool bSync)
        {
            var __ret = __Internal.SetVerticalSync(__Instance, bSync);
            return __ret;
        }

        public bool SelectSenderPanel(string message)
        {
            var __ret = __Internal.SelectSenderPanel(__Instance, message);
            return __ret;
        }

        public bool CheckSpoutPanel()
        {
            var __ret = __Internal.CheckSpoutPanel(__Instance);
            return __ret;
        }

        public bool OpenSpout()
        {
            var __ret = __Internal.OpenSpout(__Instance);
            return __ret;
        }

        public bool WritePathToRegistry(string filepath, string subkey, string valuename)
        {
            var __ret = __Internal.WritePathToRegistry(__Instance, filepath, subkey, valuename);
            return __ret;
        }

        public bool ReadPathFromRegistry(sbyte* filepath, string subkey, string valuename)
        {
            var __ret = __Internal.ReadPathFromRegistry(__Instance, filepath, subkey, valuename);
            return __ret;
        }

        public bool RemovePathFromRegistry(string subkey, string valuename)
        {
            var __ret = __Internal.RemovePathFromRegistry(__Instance, subkey, valuename);
            return __ret;
        }

        public void UseAccessLocks(bool bUseLocks)
        {
            __Internal.UseAccessLocks(__Instance, bUseLocks);
        }

        public void SpoutCleanUp(bool bExit)
        {
            __Internal.SpoutCleanUp(__Instance, bExit);
        }

        public void CleanSenders()
        {
            __Internal.CleanSenders(__Instance);
        }

        protected bool OpenReceiver(sbyte* name, ref uint width, ref uint height)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.OpenReceiver(__Instance, name, __arg1, __arg2);
                    return __ret;
                }
            }
        }

        protected bool InitMemoryShare(bool bReceiver)
        {
            var __ret = __Internal.InitMemoryShare(__Instance, bReceiver);
            return __ret;
        }

        protected bool FindFileVersion(string filepath, ref uint versMS, ref uint versLS)
        {
            fixed (uint* __versMS1 = &versMS)
            {
                var __arg1 = __versMS1;
                fixed (uint* __versLS2 = &versLS)
                {
                    var __arg2 = __versLS2;
                    var __ret = __Internal.FindFileVersion(__Instance, filepath, __arg1, __arg2);
                    return __ret;
                }
            }
        }

        public global::Spout.Interop.SpoutGLDXinterop Interop
        {
            get
            {
                return global::Spout.Interop.SpoutGLDXinterop.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.Spout.__Internal*)__Instance)->interop));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->interop = *(global::Spout.Interop.SpoutGLDXinterop.__Internal*)value.__Instance;
            }
        }

        protected sbyte[] GSharedMemoryName
        {
            get
            {
                sbyte[] __value = null;
                if (((global::Spout.Interop.Spout.__Internal*)__Instance)->g_SharedMemoryName != null)
                {
                    __value = new sbyte[256];
                    for (int i = 0; i < 256; i++)
                        __value[i] = ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_SharedMemoryName[i];
                }
                return __value;
            }

            set
            {
                if (value != null)
                {
                    for (int i = 0; i < 256; i++)
                        ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_SharedMemoryName[i] = value[i];
                }
            }
        }

        protected sbyte[] UserSenderName
        {
            get
            {
                sbyte[] __value = null;
                if (((global::Spout.Interop.Spout.__Internal*)__Instance)->UserSenderName != null)
                {
                    __value = new sbyte[256];
                    for (int i = 0; i < 256; i++)
                        __value[i] = ((global::Spout.Interop.Spout.__Internal*)__Instance)->UserSenderName[i];
                }
                return __value;
            }

            set
            {
                if (value != null)
                {
                    for (int i = 0; i < 256; i++)
                        ((global::Spout.Interop.Spout.__Internal*)__Instance)->UserSenderName[i] = value[i];
                }
            }
        }

        protected uint GWidth
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_Width;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_Width = value;
            }
        }

        protected uint GHeight
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_Height;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_Height = value;
            }
        }

        protected global::System.IntPtr GShareHandle
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_ShareHandle;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_ShareHandle = (global::System.IntPtr)value;
            }
        }

        protected uint GFormat
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_Format;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_Format = value;
            }
        }

        protected uint GTexID
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_TexID;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->g_TexID = value;
            }
        }

        protected bool BGLDXcompatible
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bGLDXcompatible != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bGLDXcompatible = (byte)(value ? 1 : 0);
            }
        }

        protected bool BMemoryShareInitOK
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bMemoryShareInitOK != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bMemoryShareInitOK = (byte)(value ? 1 : 0);
            }
        }

        protected bool BDxInitOK
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bDxInitOK != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bDxInitOK = (byte)(value ? 1 : 0);
            }
        }

        protected bool BUseCPU
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bUseCPU != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bUseCPU = (byte)(value ? 1 : 0);
            }
        }

        protected bool BMemory
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bMemory != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bMemory = (byte)(value ? 1 : 0);
            }
        }

        protected bool BInitialized
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bInitialized != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bInitialized = (byte)(value ? 1 : 0);
            }
        }

        protected bool BIsSending
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bIsSending != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bIsSending = (byte)(value ? 1 : 0);
            }
        }

        protected bool BIsReceiving
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bIsReceiving != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bIsReceiving = (byte)(value ? 1 : 0);
            }
        }

        protected bool BChangeRequested
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bChangeRequested != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bChangeRequested = (byte)(value ? 1 : 0);
            }
        }

        protected bool BSpoutPanelOpened
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bSpoutPanelOpened != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bSpoutPanelOpened = (byte)(value ? 1 : 0);
            }
        }

        protected bool BSpoutPanelActive
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bSpoutPanelActive != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bSpoutPanelActive = (byte)(value ? 1 : 0);
            }
        }

        protected bool BUseActive
        {
            get
            {
                return ((global::Spout.Interop.Spout.__Internal*)__Instance)->bUseActive != 0;
            }

            set
            {
                ((global::Spout.Interop.Spout.__Internal*)__Instance)->bUseActive = (byte)(value ? 1 : 0);
            }
        }

        public bool UnBindSharedTexture
        {
            get
            {
                var __ret = __Internal.UnBindSharedTexture(__Instance);
                return __ret;
            }
        }

        public int SenderCount
        {
            get
            {
                var __ret = __Internal.GetSenderCount(__Instance);
                return __ret;
            }
        }

        public bool DX9
        {
            get
            {
                var __ret = __Internal.GetDX9(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetDX9(__Instance, value);
            }
        }

        public bool MemoryShareMode
        {
            get
            {
                var __ret = __Internal.GetMemoryShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetMemoryShareMode(__Instance, value);
            }
        }

        public bool CPUmode
        {
            get
            {
                var __ret = __Internal.GetCPUmode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetCPUmode(__Instance, value);
            }
        }

        public int ShareMode
        {
            get
            {
                var __ret = __Internal.GetShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetShareMode(__Instance, value);
            }
        }

        public int MaxSenders
        {
            get
            {
                var __ret = __Internal.GetMaxSenders(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetMaxSenders(__Instance, value);
            }
        }

        public bool IsSpoutInitialized
        {
            get
            {
                var __ret = __Internal.IsSpoutInitialized(__Instance);
                return __ret;
            }
        }

        public bool IsBGRAavailable
        {
            get
            {
                var __ret = __Internal.IsBGRAavailable(__Instance);
                return __ret;
            }
        }

        public bool IsPBOavailable
        {
            get
            {
                var __ret = __Internal.IsPBOavailable(__Instance);
                return __ret;
            }
        }

        public bool BufferMode
        {
            get
            {
                var __ret = __Internal.GetBufferMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetBufferMode(__Instance, value);
            }
        }

        public int NumAdapters
        {
            get
            {
                var __ret = __Internal.GetNumAdapters(__Instance);
                return __ret;
            }
        }

        public int Adapter
        {
            get
            {
                var __ret = __Internal.GetAdapter(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetAdapter(__Instance, value);
            }
        }

        public int VerticalSync
        {
            get
            {
                var __ret = __Internal.GetVerticalSync(__Instance);
                return __ret;
            }
        }

        public int ReportMemory
        {
            get
            {
                var __ret = __Internal.ReportMemory(__Instance);
                return __ret;
            }
        }

        protected bool GLDXcompatible
        {
            get
            {
                var __ret = __Internal.GLDXcompatible(__Instance);
                return __ret;
            }
        }

        protected bool ReleaseMemoryShare
        {
            get
            {
                var __ret = __Internal.ReleaseMemoryShare(__Instance);
                return __ret;
            }
        }
    }

    public unsafe partial class SpoutSender : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 1336)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::Spout.Interop.Spout.__Internal spout;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SpoutSender@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SpoutSender@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1SpoutSender@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateSender@SpoutSender@@QEAA_NPEBDIIK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername, uint width, uint height, uint dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UpdateSender@SpoutSender@@QEAA_NPEBDII@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UpdateSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername, uint width, uint height);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseSender@SpoutSender@@QEAAXK@Z")]
            internal static extern void ReleaseSender(global::System.IntPtr __instance, uint dwMsec);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SendImage@SpoutSender@@QEAA_NPEBEIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SendImage(global::System.IntPtr __instance, byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SendTexture@SpoutSender@@QEAA_NIIII_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SendTexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawToSharedTexture@SpoutSender@@QEAA_NIIIIMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawToSharedTexture(global::System.IntPtr __instance, uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SelectSenderPanel@SpoutSender@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SelectSenderPanel(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetDX9@SpoutSender@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetDX9(global::System.IntPtr __instance, bool bDX9);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetMemoryShareMode@SpoutSender@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetMemoryShareMode(global::System.IntPtr __instance, bool bMem);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetCPUmode@SpoutSender@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetCPUmode(global::System.IntPtr __instance, bool bCPU);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetShareMode@SpoutSender@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetShareMode(global::System.IntPtr __instance, int mode);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterName@SpoutSender@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterName(global::System.IntPtr __instance, int index, sbyte* adaptername, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetAdapter@SpoutSender@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetAdapter(global::System.IntPtr __instance, int index);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetHostPath@SpoutSender@@QEAA_NPEBDPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetHostPath(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, sbyte* hostpath, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetVerticalSync@SpoutSender@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetVerticalSync(global::System.IntPtr __instance, bool bSync);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SenderDebug@SpoutSender@@QEAA_NPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SenderDebug(global::System.IntPtr __instance, sbyte* Sendername, int size);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetDX9@SpoutSender@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetDX9(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetMemoryShareMode@SpoutSender@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetMemoryShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetCPUmode@SpoutSender@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetCPUmode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetShareMode@SpoutSender@@QEAAHXZ")]
            internal static extern int GetShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetBufferMode@SpoutSender@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetBufferMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetBufferMode@SpoutSender@@QEAAX_N@Z")]
            internal static extern void SetBufferMode(global::System.IntPtr __instance, bool bActive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetDX9compatible@SpoutSender@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetDX9compatible(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetDX9compatible@SpoutSender@@QEAAX_N@Z")]
            internal static extern void SetDX9compatible(global::System.IntPtr __instance, bool bCompatible);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetNumAdapters@SpoutSender@@QEAAHXZ")]
            internal static extern int GetNumAdapters(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapter@SpoutSender@@QEAAHXZ")]
            internal static extern int GetAdapter(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetVerticalSync@SpoutSender@@QEAAHXZ")]
            internal static extern int GetVerticalSync(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutSender> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutSender>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutSender __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutSender(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutSender __CreateInstance(global::Spout.Interop.SpoutSender.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutSender(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutSender.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSender.__Internal));
            *(global::Spout.Interop.SpoutSender.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutSender(global::Spout.Interop.SpoutSender.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutSender(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutSender()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSender.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutSender(global::Spout.Interop.SpoutSender _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutSender.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutSender.__Internal*)__Instance) = *((global::Spout.Interop.SpoutSender.__Internal*)_0.__Instance);
        }

        ~SpoutSender()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutSender __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool CreateSender(string Sendername, uint width, uint height, uint dwFormat)
        {
            var __ret = __Internal.CreateSender(__Instance, Sendername, width, height, dwFormat);
            return __ret;
        }

        public bool UpdateSender(string Sendername, uint width, uint height)
        {
            var __ret = __Internal.UpdateSender(__Instance, Sendername, width, height);
            return __ret;
        }

        public void ReleaseSender(uint dwMsec)
        {
            __Internal.ReleaseSender(__Instance, dwMsec);
        }

        public bool SendImage(byte* pixels, uint width, uint height, uint glFormat, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.SendImage(__Instance, pixels, width, height, glFormat, bInvert, HostFBO);
            return __ret;
        }

        public bool SendTexture(uint TextureID, uint TextureTarget, uint width, uint height, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.SendTexture(__Instance, TextureID, TextureTarget, width, height, bInvert, HostFBO);
            return __ret;
        }

        public bool DrawToSharedTexture(uint TextureID, uint TextureTarget, uint width, uint height, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawToSharedTexture(__Instance, TextureID, TextureTarget, width, height, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        public bool SelectSenderPanel(string message)
        {
            var __ret = __Internal.SelectSenderPanel(__Instance, message);
            return __ret;
        }

        public bool SetDX9(bool bDX9)
        {
            var __ret = __Internal.SetDX9(__Instance, bDX9);
            return __ret;
        }

        public bool SetMemoryShareMode(bool bMem)
        {
            var __ret = __Internal.SetMemoryShareMode(__Instance, bMem);
            return __ret;
        }

        public bool SetCPUmode(bool bCPU)
        {
            var __ret = __Internal.SetCPUmode(__Instance, bCPU);
            return __ret;
        }

        public bool SetShareMode(int mode)
        {
            var __ret = __Internal.SetShareMode(__Instance, mode);
            return __ret;
        }

        public bool GetAdapterName(int index, sbyte* adaptername, int maxchars)
        {
            var __ret = __Internal.GetAdapterName(__Instance, index, adaptername, maxchars);
            return __ret;
        }

        public bool SetAdapter(int index)
        {
            var __ret = __Internal.SetAdapter(__Instance, index);
            return __ret;
        }

        public bool GetHostPath(string sendername, sbyte* hostpath, int maxchars)
        {
            var __ret = __Internal.GetHostPath(__Instance, sendername, hostpath, maxchars);
            return __ret;
        }

        public bool SetVerticalSync(bool bSync)
        {
            var __ret = __Internal.SetVerticalSync(__Instance, bSync);
            return __ret;
        }

        public bool SenderDebug(sbyte* Sendername, int size)
        {
            var __ret = __Internal.SenderDebug(__Instance, Sendername, size);
            return __ret;
        }

        public global::Spout.Interop.Spout Spout
        {
            get
            {
                return global::Spout.Interop.Spout.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutSender.__Internal*)__Instance)->spout));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutSender.__Internal*)__Instance)->spout = *(global::Spout.Interop.Spout.__Internal*)value.__Instance;
            }
        }

        public bool DX9
        {
            get
            {
                var __ret = __Internal.GetDX9(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetDX9(__Instance, value);
            }
        }

        public bool MemoryShareMode
        {
            get
            {
                var __ret = __Internal.GetMemoryShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetMemoryShareMode(__Instance, value);
            }
        }

        public bool CPUmode
        {
            get
            {
                var __ret = __Internal.GetCPUmode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetCPUmode(__Instance, value);
            }
        }

        public int ShareMode
        {
            get
            {
                var __ret = __Internal.GetShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetShareMode(__Instance, value);
            }
        }

        public bool BufferMode
        {
            get
            {
                var __ret = __Internal.GetBufferMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetBufferMode(__Instance, value);
            }
        }

        public bool DX9compatible
        {
            get
            {
                var __ret = __Internal.GetDX9compatible(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetDX9compatible(__Instance, value);
            }
        }

        public int NumAdapters
        {
            get
            {
                var __ret = __Internal.GetNumAdapters(__Instance);
                return __ret;
            }
        }

        public int Adapter
        {
            get
            {
                var __ret = __Internal.GetAdapter(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetAdapter(__Instance, value);
            }
        }

        public int VerticalSync
        {
            get
            {
                var __ret = __Internal.GetVerticalSync(__Instance);
                return __ret;
            }
        }
    }

    public unsafe partial class SpoutReceiver : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 1336)]
        public partial struct __Internal
        {
            [FieldOffset(0)]
            internal global::Spout.Interop.Spout.__Internal spout;

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SpoutReceiver@@QEAA@XZ")]
            internal static extern global::System.IntPtr ctor(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??0SpoutReceiver@@QEAA@AEBV0@@Z")]
            internal static extern global::System.IntPtr cctor(global::System.IntPtr __instance, global::System.IntPtr _0);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "??1SpoutReceiver@@QEAA@XZ")]
            internal static extern void dtor(global::System.IntPtr __instance, int delete);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CreateReceiver@SpoutReceiver@@QEAA_NPEADAEAI1_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CreateReceiver(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, bool bUseActive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReceiveTexture@SpoutReceiver@@QEAA_NPEADAEAI1II_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReceiveTexture(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, uint TextureID, uint TextureTarget, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReceiveImage@SpoutReceiver@@QEAA_NPEADAEAI1PEAEI_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool ReceiveImage(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, byte* pixels, uint glFormat, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?CheckReceiver@SpoutReceiver@@QEAA_NPEADAEAI1AEA_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool CheckReceiver(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, bool* bConnected);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetImageSize@SpoutReceiver@@QEAA_NPEADAEAI1AEA_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetImageSize(global::System.IntPtr __instance, sbyte* Sendername, uint* width, uint* height, bool* bMemoryMode);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?ReleaseReceiver@SpoutReceiver@@QEAAXXZ")]
            internal static extern void ReleaseReceiver(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?BindSharedTexture@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool BindSharedTexture(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?DrawSharedTexture@SpoutReceiver@@QEAA_NMMM_NI@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool DrawSharedTexture(global::System.IntPtr __instance, float max_x, float max_y, float aspect, bool bInvert, uint HostFBO);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderName@SpoutReceiver@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderName(global::System.IntPtr __instance, int index, sbyte* Sendername, int MaxSize);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderInfo@SpoutReceiver@@QEAA_NPEBDAEAI1AEAPEAXAEAK@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetSenderInfo(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername, uint* width, uint* height, void** dxShareHandle, uint* dwFormat);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetActiveSender@SpoutReceiver@@QEAA_NPEAD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetActiveSender(global::System.IntPtr __instance, sbyte* Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetActiveSender@SpoutReceiver@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetActiveSender(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string Sendername);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SelectSenderPanel@SpoutReceiver@@QEAA_NPEBD@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SelectSenderPanel(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetDX9@SpoutReceiver@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetDX9(global::System.IntPtr __instance, bool bDX9);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetMemoryShareMode@SpoutReceiver@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetMemoryShareMode(global::System.IntPtr __instance, bool bMem);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetCPUmode@SpoutReceiver@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetCPUmode(global::System.IntPtr __instance, bool bCPU);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetShareMode@SpoutReceiver@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetShareMode(global::System.IntPtr __instance, int mode);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapterName@SpoutReceiver@@QEAA_NHPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetAdapterName(global::System.IntPtr __instance, int index, sbyte* adaptername, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetAdapter@SpoutReceiver@@QEAA_NH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetAdapter(global::System.IntPtr __instance, int index);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetHostPath@SpoutReceiver@@QEAA_NPEBDPEADH@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetHostPath(global::System.IntPtr __instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string sendername, sbyte* hostpath, int maxchars);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetVerticalSync@SpoutReceiver@@QEAA_N_N@Z")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool SetVerticalSync(global::System.IntPtr __instance, bool bSync);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?UnBindSharedTexture@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool UnBindSharedTexture(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetSenderCount@SpoutReceiver@@QEAAHXZ")]
            internal static extern int GetSenderCount(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetDX9@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetDX9(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetMemoryShareMode@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetMemoryShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetCPUmode@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetCPUmode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetShareMode@SpoutReceiver@@QEAAHXZ")]
            internal static extern int GetShareMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetBufferMode@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetBufferMode(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetBufferMode@SpoutReceiver@@QEAAX_N@Z")]
            internal static extern void SetBufferMode(global::System.IntPtr __instance, bool bActive);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetDX9compatible@SpoutReceiver@@QEAA_NXZ")]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool GetDX9compatible(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?SetDX9compatible@SpoutReceiver@@QEAAX_N@Z")]
            internal static extern void SetDX9compatible(global::System.IntPtr __instance, bool bCompatible);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetNumAdapters@SpoutReceiver@@QEAAHXZ")]
            internal static extern int GetNumAdapters(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetAdapter@SpoutReceiver@@QEAAHXZ")]
            internal static extern int GetAdapter(global::System.IntPtr __instance);

            [SuppressUnmanagedCodeSecurity]
            [DllImport("Libraries/Spout.dll", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                EntryPoint = "?GetVerticalSync@SpoutReceiver@@QEAAHXZ")]
            internal static extern int GetVerticalSync(global::System.IntPtr __instance);
        }

        public global::System.IntPtr __Instance { get; protected set; }

        internal static readonly global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutReceiver> NativeToManagedMap = new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::Spout.Interop.SpoutReceiver>();

        protected bool __ownsNativeInstance;

        internal static global::Spout.Interop.SpoutReceiver __CreateInstance(global::System.IntPtr native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutReceiver(native.ToPointer(), skipVTables);
        }

        internal static global::Spout.Interop.SpoutReceiver __CreateInstance(global::Spout.Interop.SpoutReceiver.__Internal native, bool skipVTables = false)
        {
            return new global::Spout.Interop.SpoutReceiver(native, skipVTables);
        }

        private static void* __CopyValue(global::Spout.Interop.SpoutReceiver.__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutReceiver.__Internal));
            *(global::Spout.Interop.SpoutReceiver.__Internal*)ret = native;
            return ret.ToPointer();
        }

        private SpoutReceiver(global::Spout.Interop.SpoutReceiver.__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
        }

        protected SpoutReceiver(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new global::System.IntPtr(native);
        }

        public SpoutReceiver()
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutReceiver.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            __Internal.ctor(__Instance);
        }

        public SpoutReceiver(global::Spout.Interop.SpoutReceiver _0)
        {
            __Instance = Marshal.AllocHGlobal(sizeof(global::Spout.Interop.SpoutReceiver.__Internal));
            __ownsNativeInstance = true;
            NativeToManagedMap[__Instance] = this;
            *((global::Spout.Interop.SpoutReceiver.__Internal*)__Instance) = *((global::Spout.Interop.SpoutReceiver.__Internal*)_0.__Instance);
        }

        ~SpoutReceiver()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (__Instance == IntPtr.Zero)
                return;
            global::Spout.Interop.SpoutReceiver __dummy;
            NativeToManagedMap.TryRemove(__Instance, out __dummy);
            if (disposing)
                __Internal.dtor(__Instance, 0);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public bool CreateReceiver(sbyte* Sendername, ref uint width, ref uint height, bool bUseActive)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.CreateReceiver(__Instance, Sendername, __arg1, __arg2, bUseActive);
                    return __ret;
                }
            }
        }

        public bool ReceiveTexture(sbyte* Sendername, ref uint width, ref uint height, uint TextureID, uint TextureTarget, bool bInvert, uint HostFBO)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.ReceiveTexture(__Instance, Sendername, __arg1, __arg2, TextureID, TextureTarget, bInvert, HostFBO);
                    return __ret;
                }
            }
        }

        public bool ReceiveImage(sbyte* Sendername, ref uint width, ref uint height, byte* pixels, uint glFormat, bool bInvert, uint HostFBO)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    var __ret = __Internal.ReceiveImage(__Instance, Sendername, __arg1, __arg2, pixels, glFormat, bInvert, HostFBO);
                    return __ret;
                }
            }
        }

        public bool CheckReceiver(sbyte* Sendername, ref uint width, ref uint height, ref bool bConnected)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (bool* __bConnected3 = &bConnected)
                    {
                        var __arg3 = __bConnected3;
                        var __ret = __Internal.CheckReceiver(__Instance, Sendername, __arg1, __arg2, __arg3);
                        return __ret;
                    }
                }
            }
        }

        public bool GetImageSize(sbyte* Sendername, ref uint width, ref uint height, ref bool bMemoryMode)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (bool* __bMemoryMode3 = &bMemoryMode)
                    {
                        var __arg3 = __bMemoryMode3;
                        var __ret = __Internal.GetImageSize(__Instance, Sendername, __arg1, __arg2, __arg3);
                        return __ret;
                    }
                }
            }
        }

        public void ReleaseReceiver()
        {
            __Internal.ReleaseReceiver(__Instance);
        }

        public bool BindSharedTexture()
        {
            var __ret = __Internal.BindSharedTexture(__Instance);
            return __ret;
        }

        public bool DrawSharedTexture(float max_x, float max_y, float aspect, bool bInvert, uint HostFBO)
        {
            var __ret = __Internal.DrawSharedTexture(__Instance, max_x, max_y, aspect, bInvert, HostFBO);
            return __ret;
        }

        public bool GetSenderName(int index, sbyte* Sendername, int MaxSize)
        {
            var __ret = __Internal.GetSenderName(__Instance, index, Sendername, MaxSize);
            return __ret;
        }

        public bool GetSenderInfo(string Sendername, ref uint width, ref uint height, void** dxShareHandle, ref uint dwFormat)
        {
            fixed (uint* __width1 = &width)
            {
                var __arg1 = __width1;
                fixed (uint* __height2 = &height)
                {
                    var __arg2 = __height2;
                    fixed (uint* __dwFormat4 = &dwFormat)
                    {
                        var __arg4 = __dwFormat4;
                        var __ret = __Internal.GetSenderInfo(__Instance, Sendername, __arg1, __arg2, dxShareHandle, __arg4);
                        return __ret;
                    }
                }
            }
        }

        public bool GetActiveSender(sbyte* Sendername)
        {
            var __ret = __Internal.GetActiveSender(__Instance, Sendername);
            return __ret;
        }

        public bool SetActiveSender(string Sendername)
        {
            var __ret = __Internal.SetActiveSender(__Instance, Sendername);
            return __ret;
        }

        public bool SelectSenderPanel(string message)
        {
            var __ret = __Internal.SelectSenderPanel(__Instance, message);
            return __ret;
        }

        public bool SetDX9(bool bDX9)
        {
            var __ret = __Internal.SetDX9(__Instance, bDX9);
            return __ret;
        }

        public bool SetMemoryShareMode(bool bMem)
        {
            var __ret = __Internal.SetMemoryShareMode(__Instance, bMem);
            return __ret;
        }

        public bool SetCPUmode(bool bCPU)
        {
            var __ret = __Internal.SetCPUmode(__Instance, bCPU);
            return __ret;
        }

        public bool SetShareMode(int mode)
        {
            var __ret = __Internal.SetShareMode(__Instance, mode);
            return __ret;
        }

        public bool GetAdapterName(int index, sbyte* adaptername, int maxchars)
        {
            var __ret = __Internal.GetAdapterName(__Instance, index, adaptername, maxchars);
            return __ret;
        }

        public bool SetAdapter(int index)
        {
            var __ret = __Internal.SetAdapter(__Instance, index);
            return __ret;
        }

        public bool GetHostPath(string sendername, sbyte* hostpath, int maxchars)
        {
            var __ret = __Internal.GetHostPath(__Instance, sendername, hostpath, maxchars);
            return __ret;
        }

        public bool SetVerticalSync(bool bSync)
        {
            var __ret = __Internal.SetVerticalSync(__Instance, bSync);
            return __ret;
        }

        public global::Spout.Interop.Spout Spout
        {
            get
            {
                return global::Spout.Interop.Spout.__CreateInstance(new global::System.IntPtr(&((global::Spout.Interop.SpoutReceiver.__Internal*)__Instance)->spout));
            }

            set
            {
                if (ReferenceEquals(value, null))
                    throw new global::System.ArgumentNullException("value", "Cannot be null because it is passed by value.");
                ((global::Spout.Interop.SpoutReceiver.__Internal*)__Instance)->spout = *(global::Spout.Interop.Spout.__Internal*)value.__Instance;
            }
        }

        public bool UnBindSharedTexture
        {
            get
            {
                var __ret = __Internal.UnBindSharedTexture(__Instance);
                return __ret;
            }
        }

        public int SenderCount
        {
            get
            {
                var __ret = __Internal.GetSenderCount(__Instance);
                return __ret;
            }
        }

        public bool DX9
        {
            get
            {
                var __ret = __Internal.GetDX9(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetDX9(__Instance, value);
            }
        }

        public bool MemoryShareMode
        {
            get
            {
                var __ret = __Internal.GetMemoryShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetMemoryShareMode(__Instance, value);
            }
        }

        public bool CPUmode
        {
            get
            {
                var __ret = __Internal.GetCPUmode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetCPUmode(__Instance, value);
            }
        }

        public int ShareMode
        {
            get
            {
                var __ret = __Internal.GetShareMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetShareMode(__Instance, value);
            }
        }

        public bool BufferMode
        {
            get
            {
                var __ret = __Internal.GetBufferMode(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetBufferMode(__Instance, value);
            }
        }

        public bool DX9compatible
        {
            get
            {
                var __ret = __Internal.GetDX9compatible(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetDX9compatible(__Instance, value);
            }
        }

        public int NumAdapters
        {
            get
            {
                var __ret = __Internal.GetNumAdapters(__Instance);
                return __ret;
            }
        }

        public int Adapter
        {
            get
            {
                var __ret = __Internal.GetAdapter(__Instance);
                return __ret;
            }

            set
            {
                __Internal.SetAdapter(__Instance, value);
            }
        }

        public int VerticalSync
        {
            get
            {
                var __ret = __Internal.GetVerticalSync(__Instance);
                return __ret;
            }
        }
    }
}

namespace Std
{
    namespace pair
    {
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        public unsafe partial struct __Internalc__N_std_S_pair__1__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C____S_SpoutSharedMemory
        {
            [FieldOffset(0)]
            internal global::Std.BasicString.__Internalc__N_std_S_basic_string__C___N_std_S_char_traits__C___N_std_S_allocator__C first;

            [FieldOffset(32)]
            internal global::System.IntPtr second;
        }
    }
}
