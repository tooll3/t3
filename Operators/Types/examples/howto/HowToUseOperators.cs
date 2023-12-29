using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fc3a89d4_e7ea_45d8_b1c5_41170c7cd2b8
{
    public class HowToUseOperators: Instance<HowToUseOperators>
    {
        [Output(Guid = "2621700c-8798-40ea-9dc8-a3f5ed1d0f41")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

