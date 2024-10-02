using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes.Vector;

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct Int3 : IEquatable<Int3>, IFormattable
{
    public int X, Y, Z;
    
    public int Width => X;
    public int Height => Y;
    public int Depth => Z;
    
    public Int3(int val)
    {
        X = val;
        Y = val;
        Z = val;
    }

    public Int3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public Int3 (Int2 xy, int z)
    {
        X = xy.X;
        Y = xy.Y;
        Z = z;
    }

    public Int3(Vector3 v)
    {
        X = (int)v.X;
        Y = (int)v.Y;
        Z = (int)v.Z;
    }

    // operators
    public static Int3 operator *(Int3 a, Int3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Int3 operator *(int a, Int3 b) => new(a * b.X, a * b.Y, a * b.Z);
    public static Int3 operator *(Int3 a, int b) => new(a.X * b, a.Y * b, a.Z * b);
    public static Vector3 operator *(Int3 a, float b) => (Vector3)a * b;
    public static Vector3 operator *(float a, Int3 b) => (Vector3)b * a;
    public static Vector3 operator *(Int3 a, Vector3 b) => (Vector3)a * b;

    public static Int3 operator /(Int3 a, Int3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    public static Int3 operator /(Int3 a, int b) => new(a.X / b, a.Y / b, a.Z / b);
    public static Int3 operator /(int a, Int3 b) => new(a / b.X, a / b.Y, a / b.Z);
    public static Vector3 operator /(Int3 a, float b) => (Vector3)a / b;
    public static Vector3 operator /(float a, Int3 b) => (Vector3)b / a;
    public static Vector3 operator /(Int3 a, Vector3 b) => (Vector3)a / b;

    public static Int3 operator +(Int3 a, Int3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Int3 operator +(Int3 a, int b) => new(a.X + b, a.Y + b, a.Z + b);
    public static Int3 operator +(int a, Int3 b) => new(a + b.X, a + b.Y, a + b.Z);

    public static Int3 operator -(Int3 a) => new(-a.X, -a.Y, -a.Z);
    public static Int3 operator -(Int3 a, Int3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Int3 operator -(Int3 a, int b) => new(a.X - b, a.Y - b, a.Z - b);
    public static Int3 operator -(int a, Int3 b) => new(a - b.X, a - b.Y, a - b.Z);

    public static Int3 operator %(Int3 a, Int3 b) => new(a.X % b.X, a.Y % b.Y, a.Z % b.Z);
    public static Int3 operator %(Int3 a, int b) => new(a.X % b, a.Y % b, a.Z % b);
    public static Int3 operator %(int a, Int3 b) => new(a % b.X, a % b.Y, a % b.Z);

    public static Int3 operator ++(Int3 a) => new(a.X + 1, a.Y + 1, a.Z + 1);
    public static Int3 operator --(Int3 a) => new(a.X - 1, a.Y - 1, a.Z - 1);

    public static bool operator ==(Int3 a, Int3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Int3 a, Int3 b) => !(a == b);

    public static implicit operator Vector3(Int3 a) => new(a.X, a.Y, a.Z);
    public static implicit operator Vector4(Int3 a) => new(a.X, a.Y, a.Z, 0);

    public static explicit operator Int3(Vector2 a) => new((int)a.X, (int)a.Y, 0);
    public static explicit operator Int3(Vector3 a) => new((int)a.X, (int)a.Y, (int)a.Z);

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}, {Z.ToString(format, formatProvider)})";
    }

    public bool Equals(Int3 other) => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object obj) => obj is Int3 int3 && Equals(int3);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    
    public static readonly int SizeInBytes = Marshal.SizeOf<Int3>();
    public static readonly Int3 Zero = new(0, 0, 0);
    public static readonly Int3 UnitX = new(1, 0, 0);
    public static readonly Int3 UnitY = new(0, 1, 0);
    public static readonly Int3 UnitZ = new(0, 0, 1);
    public static readonly Int3 One = new(1, 1, 1);
}