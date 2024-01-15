using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes
{
    [StructLayout(LayoutKind.Explicit, Size = Stride)]
    public struct Point
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(3 * 4)]
        public float W;

        [FieldOffset(4 * 4)]
        public Quaternion Orientation;

        [FieldOffset(8 * 4)]
        public Vector4 Color;
        
        [FieldOffset(12 * 4)]
        public Vector3 Stretch;
        
        [FieldOffset(15 * 4)]
        public float Selected;

        public static Point Separator()
        {
            return new Point
                       {
                           Position = Vector3.Zero,
                           W = float.NaN,
                           Orientation = Quaternion.Identity,
                           Color = Vector4.One,
                           Stretch = Vector3.One,
                           Selected = 0,
                       };
        }

        public const int Stride = 16 * 4;
    }
}