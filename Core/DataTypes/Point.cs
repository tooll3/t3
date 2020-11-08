using System.Runtime.InteropServices;

namespace T3.Core.DataTypes
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Point
    {
        [FieldOffset(0)]
        public SharpDX.Vector4 Position;
        // [FieldOffset(16)]
        // public SharpDX.Vector4 Orientation;
    }
}