using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.DataTypes
{
    /// <summary>
    /// Combines buffers required for mesh rendering
    /// </summary>
    public class ParticleSystem
    {
        public BufferWithViews PointBuffer;
        public BufferWithViews PointSimBuffer;
        public float SpeedFactor;
        public float InitializeVelocityFactor;
    }
    
    /// <summary>
    /// Hold additional point information require for particle simulations.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 1 * 16)]
    public struct ParticlePoint
    {
        [FieldOffset(0)]
        public Vector3 Velocity;

        [FieldOffset(12)]
        public float BirthTime;

        public static ParticlePoint Separator()
        {
            return new ParticlePoint
                       {
                           Velocity = Vector3.Zero,
                           BirthTime = 0,
                       };
        }
    }
}