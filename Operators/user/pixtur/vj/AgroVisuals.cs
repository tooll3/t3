using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3ec672b7_f794_49a8_a5e1_e04c927f2ac5
{
    public class AgroVisuals : Instance<AgroVisuals>
    {
        [Output(Guid = "27b913ca-2ac8-4565-830b-c2cbb4783939")]
        public readonly Slot<Texture2D> Output = new();


    }
}

