using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f421e8d5_0965_461c_8676_ed40d1cca381
{
    public class Downsample : Instance<Downsample>
    {
        [Output(Guid = "ecf8efec-4d53-4dac-80c4-4d300285a679")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "edb5145d-f229-4028-bb85-d78bf59ef70a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Color = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "d5fdfb0e-f714-41e6-963d-4154eedc4a26")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> CoC = new InputSlot<SharpDX.Direct3D11.Texture2D>();

    }
}

