using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_90b20942_810b_480c_a19e_a41296cac9e6
{
    public class WS1_Searching : Instance<WS1_Searching>
    {
        [Output(Guid = "8902091c-9eee-4a4e-a55e-67768ba3465a")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "e75b329f-26ae-4070-91bc-450c30d7453d")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new();


    }
}

