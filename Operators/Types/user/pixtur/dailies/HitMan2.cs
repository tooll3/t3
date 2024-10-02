using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fe9ef18c_7780_42f4_bf25_d37b21ea7c52
{
    public class HitMan2 : Instance<HitMan2>
    {
        [Output(Guid = "fd5c5695-2898-4027-b0cb-a719c06f5257")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

