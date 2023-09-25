using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes
{
    /// <summary>
    /// Combines buffers required for mesh rendering
    /// </summary>
    public class ParticleSystem
    {
        public BufferWithViews ParticleBuffer;
        public float SpeedFactor;
        public float InitializeVelocityFactor;
    }
    
    /// <summary>
    /// Hold additional point information require for particle simulations.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16 * 4)]
    public struct ParticlePoint
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(3*4)]
        public float BirthTime;

        [FieldOffset(4*4)]
        public Vector3 Velocity;

        [FieldOffset(7*4)]
        public float Radius;

        [FieldOffset(8*4)]
        public Quaternion Rotation;

        [FieldOffset(12*4)]
        public Vector4 __extra;
        
        public static ParticlePoint Separator()
        {
            return new ParticlePoint
                       {
                           Velocity = Vector3.Zero,
                           BirthTime = 0,
                           Radius = 1,
                       };
        }
    }
}