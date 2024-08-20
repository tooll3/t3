using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_787f44a8_8c51_4cfa_a7d5_7014d11b6a28
{
    public class GetImageBrightness : Instance<GetImageBrightness>
    {

        [Output(Guid = "4a1be0ac-96af-459b-b31a-2fd1373964ab")]
        public readonly Slot<T3.Core.DataTypes.Command> Update = new Slot<T3.Core.DataTypes.Command>();

        [Output(Guid = "0edd2f73-be22-4164-9d0f-0552faddb094")]
        public readonly Slot<float> Brightness = new Slot<float>();

        [Input(Guid = "380ee818-52f2-4eed-9a6b-4a44a90abd7b")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new();

    }
}