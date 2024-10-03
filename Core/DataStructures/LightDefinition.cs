using System.Numerics;
using System.Runtime.InteropServices;

namespace T3.Core.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = StructSize)]
    public struct LightDefinition
    {
        public LightDefinition(System.Numerics.Vector3 position,
                               float intensity,
                               System.Numerics.Vector4 color,
                               float range,
                               float decay = 2)
        {
            Position = position;
            Intensity = intensity;
            Color = color;
            Range = range;
            Decay = decay;
            SpotLightDirection = Vector3.UnitZ;
            SpotLightFOV = 1;
            SpotLightEdge = 0;
            ShadowMode = 0;
        }

        [FieldOffset(0)]
        public System.Numerics.Vector3 Position;

        [FieldOffset(3 * 4)]
        public float Intensity;

        [FieldOffset(4 * 4)]
        public System.Numerics.Vector4 Color;

        [FieldOffset(8 * 4)]
        public System.Numerics.Vector3 SpotLightDirection;
        
        [FieldOffset(11 * 4)]
        public float Range;
        
        [FieldOffset(12 * 4)]
        public float Decay;
        
        [FieldOffset(13 * 4)]
        public float SpotLightFOV;  

        [FieldOffset(14 * 4)]
        public float SpotLightEdge;  

        [FieldOffset(15 * 4)]
        public int ShadowMode; // 0 off / 1 cast shadow 

        public const int StructSize = 16 * 4;
    }
    
}