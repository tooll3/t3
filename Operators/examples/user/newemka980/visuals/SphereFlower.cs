using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_40a73341_0210_4d77_b893_b57dfd3d9d90
{
    public class SphereFlower : Instance<SphereFlower>
    {
        [Output(Guid = "0527ab8f-e0a6-4630-a4dc-61cf41a47581")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

