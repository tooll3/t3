using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d6304632_a8c5_4029_8087_dc992b1f899c
{
    public class SynthTiling : Instance<SynthTiling>
    {

        [Output(Guid = "79c9eb66-e495-48e1-ae38-8410721ea1c5")]
        public readonly Slot<T3.Core.DataTypes.Command> Output = new();

        [Input(Guid = "99b68dc5-4b7d-4def-8697-a0c1f7621604")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();


    }
}

