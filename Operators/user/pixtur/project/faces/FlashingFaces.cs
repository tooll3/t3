using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f487a6ae_62ed_4f35_9ca8_22f6a25fc2cc
{
    public class FlashingFaces : Instance<FlashingFaces>
    {
        [Output(Guid = "92c2800f-e565-4b7a-bf7d-83117d26f8af")]
        public readonly Slot<Texture2D> Output = new();


    }
}

