using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_08e37776_9971_4ebe_a97a_90a0297e7e76
{
    public class PDFExample : Instance<PDFExample>
    {

        [Output(Guid = "b95e5264-1308-4764-8adf-d6d0d3780da8")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Out = new Slot<SharpDX.Direct3D11.Texture2D>();

    }
}

