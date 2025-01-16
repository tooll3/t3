using System;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes.Vector;

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct Int4 : IEquatable<Int4>, IFormattable
{
    public int X, Y, Z, W;
    
    public int Width => X;
    public int Height => Y;
    public int Depth => Z;
    
    public Int4(int val)
    {
        X = val;
        Y = val;
        Z = val;
        W = val;
    }
    
    public Int4 (Int2 xy, int z, int w)
    {
        X = xy.X;
        Y = xy.Y;
        Z = z;
        W = w;
    }
    
    public Int4 (Int3 xyz, int w)
    {
        X = xyz.X;
        Y = xyz.Y;
        Z = xyz.Z;
        W = w;
    }

    public Int4(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Int4(Vector4 v)
    {
        X = (int)v.X;
        Y = (int)v.Y;
        Z = (int)v.Z;
        W = (int)v.W;
    }

    // operators
    public static Int4 operator *(Int4 a, Int4 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    public static Int4 operator *(int a, Int4 b) => new(a * b.X, a * b.Y, a * b.Z, a * b.W);
    public static Int4 operator *(Int4 a, int b) => new(a.X * b, a.Y * b, a.Z * b, a.W * b);
    public static Vector4 operator *(Int4 a, float b) => (Vector4)a * b;
    public static Vector4 operator *(float a, Int4 b) => (Vector4)b * a;
    public static Vector4 operator *(Int4 a, Vector4 b) => (Vector4)a * b;

    public static Int4 operator /(Int4 a, Int4 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
    public static Int4 operator /(Int4 a, int b) => new(a.X / b, a.Y / b, a.Z / b, a.W / b);
    public static Int4 operator /(int a, Int4 b) => new(a / b.X, a / b.Y, a / b.Z, a / b.W);
    public static Vector4 operator /(Int4 a, float b) => (Vector4)a / b;
    public static Vector4 operator /(float a, Int4 b) => (Vector4)b / a;
    public static Vector4 operator /(Int4 a, Vector4 b) => (Vector4)a / b;

    public static Int4 operator +(Int4 a, Int4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Int4 operator +(Int4 a, int b) => new(a.X + b, a.Y + b, a.Z + b, a.W + b);
    public static Int4 operator +(int a, Int4 b) => new(a + b.X, a + b.Y, a + b.Z, a + b.W);

    public static Int4 operator -(Int4 a) => new(-a.X, -a.Y, -a.Z, -a.W);
    public static Int4 operator -(Int4 a, Int4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    public static Int4 operator -(Int4 a, int b) => new(a.X - b, a.Y - b, a.Z - b, a.W - b);
    public static Int4 operator -(int a, Int4 b) => new(a - b.X, a - b.Y, a - b.Z, a - b.W);

    public static Int4 operator %(Int4 a, Int4 b) => new(a.X % b.X, a.Y % b.Y, a.Z % b.Z, a.W % b.W);
    public static Int4 operator %(Int4 a, int b) => new(a.X % b, a.Y % b, a.Z % b, a.W % b);
    public static Int4 operator %(int a, Int4 b) => new(a % b.X, a % b.Y, a % b.Z, a % b.W);

    public static Int4 operator ++(Int4 a) => new(a.X + 1, a.Y + 1, a.Z + 1, a.W + 1);
    public static Int4 operator --(Int4 a) => new(a.X - 1, a.Y - 1, a.Z - 1, a.W - 1);

    public static bool operator ==(Int4 a, Int4 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
    public static bool operator !=(Int4 a, Int4 b) => !(a == b);

    public static implicit operator Vector4(Int4 a) => new(a.X, a.Y, a.Z, a.W);

    public static explicit operator Int4(Vector2 a) => new((int)a.X, (int)a.Y, 0, 0);
    public static explicit operator Int4(Vector3 a) => new((int)a.X, (int)a.Y, (int)a.Z, 0);
    public static explicit operator Int4(Vector4 a) => new((int)a.X, (int)a.Y, (int)a.Z, (int)a.W);

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return
            $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}, {Z.ToString(format, formatProvider)}, {W.ToString(format, formatProvider)})";
    }

    public bool Equals(Int4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    public override bool Equals(object obj) => obj is Int4 int4 && Equals(int4);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    
    public static readonly int SizeInBytes = Marshal.SizeOf<Int4>();
    public static readonly Int4 Zero = new(0, 0, 0, 0);
    public static readonly Int4 UnitX = new(1, 0, 0, 0);
    public static readonly Int4 UnitY = new(0, 1, 0, 0);
    public static readonly Int4 UnitZ = new(0, 0, 1, 0);
    public static readonly Int4 UnitW = new(0, 0, 0, 1);
    public static readonly Int4 One = new(1, 1, 1, 1);
}