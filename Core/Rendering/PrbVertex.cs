using System.Runtime.InteropServices;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;

namespace T3.Core.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = Stride)]
    public struct PbrVertex
    {
        [FieldOffset(0)]
        public Vector3 Position;  // 3 floats -> 12 bytes

        [FieldOffset(3 * 4)]
        public Vector3 Normal;    // 3 floats -> 12 bytes

        [FieldOffset(6 * 4)]
        public Vector3 Tangent;   // 3 floats -> 12 bytes

        [FieldOffset(9 * 4)]
        public Vector3 Bitangent; // 3 floats -> 12 bytes

        [FieldOffset(12 * 4)]
        public Vector2 Texcoord;  // 2 floats -> 8 bytes

        [FieldOffset(14 * 4)]
        public Vector2 Texcoord2; // 2 floats -> 8 bytes (newly added second UV set)

        [FieldOffset(16 * 4)]
        public float Selection;   // 1 float -> 4 bytes

        [FieldOffset(17 * 4)]
        private float __padding;  // 1 float -> 4 bytes (for alignment)

        public const int Stride = 18 * 4; // Total size: 18 floats -> 72 bytes
    }
}
