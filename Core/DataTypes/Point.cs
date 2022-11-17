using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes
{
    [StructLayout(LayoutKind.Explicit, Size = 2 * 16)]
    public struct Point
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float W;

        [FieldOffset(16)]
        public Quaternion Orientation;

        public static Point Separator()
        {
            return new Point
                       {
                           Position = Vector3.Zero,
                           W = Single.NaN,
                           Orientation = Quaternion.Identity
                       };
        }
    }
}