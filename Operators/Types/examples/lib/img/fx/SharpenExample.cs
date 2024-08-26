using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_eccd22ed_1a59_4655_b811_10790871cd4c
{
    public class SharpenExample : Instance<SharpenExample>
    {
        [Output(Guid = "edc2dd9c-0a39-42a5-8af5-ea67e639d2f3")]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();


    }
}

