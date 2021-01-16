using System.Runtime.InteropServices;

namespace T3.Core.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = Stride)]
    public struct PbrVertex
    {
        [FieldOffset(0)]
        public SharpDX.Vector3 Position;

        [FieldOffset(3 * 4)]
        public SharpDX.Vector3 Normal;

        [FieldOffset(6 * 4)]
        public SharpDX.Vector3 Tangent;

        [FieldOffset(9 * 4)]
        public SharpDX.Vector3 Bitangent;

        [FieldOffset(12 * 4)]
        public SharpDX.Vector2 Texcoord;

        [FieldOffset(14 * 4)]
        private SharpDX.Vector2 __padding; //Todo: clarify if 16 byte padding is required 

        public const int Stride = 16 * 4;
    }

    [StructLayout(LayoutKind.Explicit, Size = 4 * 4)]
    public struct PbrFace
    {
        [FieldOffset(0)]
        public SharpDX.Int3 VertexIndices;

        [FieldOffset(3 * 4)]
        private float __padding;
    }
}