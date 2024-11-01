using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes;

/// <summary>
/// Combines buffers required for mesh rendering
/// </summary>
public class ParticleSystem
{
    public BufferWithViews ParticleBuffer;
    public float SpeedFactor;
    public bool IsReset;
    public float InitializeVelocityFactor;
}

// /// <summary>
// /// Hold additional point information require for particle simulations.
// /// </summary>
[StructLayout(LayoutKind.Explicit, Size = 16 * 4)]
public struct ParticlePoint
{
    [FieldOffset(0)]
    public Vector3 Position;
    
    [FieldOffset(3 * 4)]
    public float W;
    
    [FieldOffset(4 * 4)]
    public Quaternion Rotation;
    
    [FieldOffset(8 * 4)]
    public Vector4 Color;
    
    [FieldOffset(12 * 4)]
    public Vector3 Velocity;
    
    [FieldOffset(15 * 4)]
    public float Extra;
    
    public static ParticlePoint Separator()
    {
        return new ParticlePoint
                   {
                       Velocity = Vector3.Zero,
                       Extra = 0,
                       W = 1,
                   };
    }
}