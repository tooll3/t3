using System.Runtime.InteropServices;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;

namespace T3.Core.Rendering;

[StructLayout(LayoutKind.Explicit, Size = Stride)]
public struct PbrVertex
{
    [FieldOffset(0)]
    public Vector3 Position;

    [FieldOffset(3 * 4)]
    public Vector3 Normal;

    [FieldOffset(6 * 4)]
    public Vector3 Tangent;

    [FieldOffset(9 * 4)]
    public Vector3 Bitangent;

    [FieldOffset(12 * 4)]
    public Vector2 Texcoord;

    [FieldOffset(14 * 4)]
    public float Selection;

    [FieldOffset(15 * 4)]
    private float __padding;

    public const int Stride = 16 * 4;
}