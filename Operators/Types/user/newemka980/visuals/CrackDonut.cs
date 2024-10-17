using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fe8c2982_0d20_4dc4_afa1_cbf2fb08b039
{
    public class CrackDonut : Instance<CrackDonut>
    {

        [Output(Guid = "0382c2a7-d3ba-49fa-a127-d9e1d6d57d00")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOut = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "deeee56f-aead-418e-a95d-64107810a878")]
        public readonly InputSlot<bool> Merge = new InputSlot<bool>();

    }
}

