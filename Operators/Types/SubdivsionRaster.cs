using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f15cc064_1d70_4945_ae60_35d884788c0f
{
    public class SubdivsionRaster : Instance<SubdivsionRaster>
    {

        [Output(Guid = "FBDF0455-001D-4226-B89F-9D2DBFFCA515")]
        public readonly Slot<Texture2D> OutBuffer = new Slot<Texture2D>();
        
        
        [Input(Guid = "971d44e4-fc34-4dc2-9e94-e6cc202b1ef6")]
        public readonly InputSlot<Size2> Int2 = new InputSlot<Size2>();

        [Input(Guid = "e618891e-2cdb-4464-8f72-577a11b0bb14")]
        public readonly InputSlot<Texture2D> Image = new InputSlot<Texture2D>();

        [Input(Guid = "70a7d763-a8b3-45e7-8f7a-24ec27a31ba6")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();

        [Input(Guid = "3332b1e1-282d-4c41-9fe3-e609611ade51")]
        public readonly InputSlot<float> LineWidth = new InputSlot<float>();

    }
}

