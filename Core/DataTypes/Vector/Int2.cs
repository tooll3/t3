using System;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes.Vector;

// Todo - generic math
[Serializable, StructLayout(LayoutKind.Sequential)]
public struct Int2 : IEquatable<Int2>, IFormattable
{
    public int X, Y;
    
    public int Width
    {
        get => X;
        set => X = value;
    }

    public int Height 
    {
        get => Y;
        set => Y = value;
    }

    public Int2(int val)
    {
        X = val;
        Y = val;
    }

    public Int2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Int2(Vector2 v)
    {
        X = (int)v.X;
        Y = (int)v.Y;
    }

    // operators
    public static Int2 operator *(Int2 a, Int2 b) => new(a.X * b.X, a.Y * b.Y);
    public static Int2 operator *(int a, Int2 b) => new(a * b.X, a * b.Y);
    public static Int2 operator *(Int2 a, int b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(Int2 a, float b) => (Vector2)a * b;
    public static Vector2 operator *(float a, Int2 b) => (Vector2)b * a;
    public static Vector2 operator *(Int2 a, Vector2 b) => (Vector2)a * b;

    public static Int2 operator /(Int2 a, Int2 b) => new(a.X / b.X, a.Y / b.Y);
    public static Int2 operator /(Int2 a, int b) => new(a.X / b, a.Y / b);
    public static Int2 operator /(int a, Int2 b) => new(a / b.X, a / b.Y);
    public static Vector2 operator /(Int2 a, float b) => (Vector2)a / b;
    public static Vector2 operator /(float a, Int2 b) => (Vector2)b / a;
    public static Vector2 operator /(Int2 a, Vector2 b) => (Vector2)a / b;

    public static Int2 operator +(Int2 a, Int2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Int2 operator +(Int2 a, int b) => new(a.X + b, a.Y + b);
    public static Int2 operator +(int a, Int2 b) => new(a + b.X, a + b.Y);

    public static Int2 operator -(Int2 a) => new(-a.X, -a.Y);
    public static Int2 operator -(Int2 a, Int2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Int2 operator -(Int2 a, int b) => new(a.X - b, a.Y - b);
    public static Int2 operator -(int a, Int2 b) => new(a - b.X, a - b.Y);

    public static Int2 operator %(Int2 a, Int2 b) => new(a.X % b.X, a.Y % b.Y);
    public static Int2 operator %(Int2 a, int b) => new(a.X % b, a.Y % b);
    public static Int2 operator %(int a, Int2 b) => new(a % b.X, a % b.Y);

    public static Int2 operator ++(Int2 a) => new(a.X + 1, a.Y + 1);
    public static Int2 operator --(Int2 a) => new(a.X - 1, a.Y - 1);

    public static bool operator ==(Int2 a, Int2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Int2 a, Int2 b) => !(a == b);

    public static implicit operator Vector2(Int2 a) => new(a.X, a.Y);
    public static implicit operator Vector3(Int2 a) => new(a.X, a.Y, 0);
    public static implicit operator Vector4(Int2 a) => new(a.X, a.Y, 0, 0);

    public static explicit operator Int2(Vector2 a) => new((int)a.X, (int)a.Y);

    public string ToString(string format, IFormatProvider formatProvider) => $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)})";

    public bool Equals(Int2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object obj) => obj is Int2 int2 && Equals(int2);

    public override int GetHashCode() => HashCode.Combine(X, Y);
    
    public static readonly int SizeInBytes = Marshal.SizeOf<Int2>();
    public static readonly Int2 Zero = new(0, 0);
    public static readonly Int2 UnitX = new(1, 0);
    public static readonly Int2 UnitY = new(0, 1);
    public static readonly Int2 One = new(1, 1);
}