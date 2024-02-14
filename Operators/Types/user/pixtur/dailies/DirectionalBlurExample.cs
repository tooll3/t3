using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b01a8336_6dcc_45cd_a86b_a0880772f9a9
{
    public class DirectionalBlurExample : Instance<DirectionalBlurExample>
    {
        [Output(Guid = "623bd104-bf6f-416b-b9c0-d8022e87845e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

