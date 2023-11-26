using System;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes.Vector;

/// <summary>
/// This is an evil little struct that can be used to store 4 bytes that can be read as a single int/uint,
/// or you can simply use the 4 bytes as you would in a vector.
/// If you want to access individual bytes of an int or uint, use the corresponding constructor and access the X, Y, Z, W fields
/// </summary>
[Serializable, StructLayout(LayoutKind.Explicit)]
public readonly struct Byte4
{
    [FieldOffset(0)]
    public readonly byte X = 0; // byte 1

    [FieldOffset(1)]
    public readonly byte Y = 0; // byte 2

    [FieldOffset(2)]
    public readonly byte Z = 0; // byte 3

    [FieldOffset(3)]
    public readonly byte W = 0; // byte 4

    // This is a cute trick to evaluate the bytes as an int
    [FieldOffset(0)]
    public readonly int Int = 0;
    
    // Same as above, but as an unsigned int
    [FieldOffset(0)]
    public readonly uint UInt = 0;
    
    // Same as above, but as a float
    [FieldOffset(0)]
    public readonly float Float = 0;

    // ---- constructors ---- \\
    public Byte4(byte x, byte y, byte z, byte w)
    {
        X = x; 
        Y = y; 
        Z = z;
        W = w;
    }

    public Byte4(int value) => Int = value;
    public Byte4(uint value) => UInt = value;
    public Byte4(float value) => Float = value;

    // ---- implicit conversions ---- \\
    public static implicit operator int(Byte4 value) => value.Int;
    public static implicit operator Byte4(int value) => new(value);
    
    public static implicit operator uint(Byte4 value) => value.UInt;
    public static implicit operator Byte4(uint value) => new(value);
}