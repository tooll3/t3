using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a5de95e9_008c_4a26_b9f5_65e842d16d74
{
    public class MapImageToResolution : Instance<MapImageToResolution>
    {
        [Output(Guid = "7005e7bf-1a9e-4893-8b17-a05b8845532d")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        [Input(Guid = "13fd6036-aa8c-4694-9c7b-0f4cbeb74b7b")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "66ef98f5-9d92-479a-8124-65ee0ce366fe")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

    }
}

