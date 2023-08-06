using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b5b6c046_3c8e_478a_b423_899872c2e1c4
{
    public class KeepPreviousFrame : Instance<KeepPreviousFrame>
    {
        [Output(Guid = "4cf4e43b-0f1f-41f7-9ba3-acab3b1160cb")]
        public readonly Slot<Texture2D> TextureA = new Slot<Texture2D>();

        [Output(Guid = "edc79491-f0c1-47c6-bc70-8014ebdb1a7a")]
        public readonly Slot<Texture2D> TextureB = new Slot<Texture2D>();


        [Input(Guid = "216dca25-9ba2-4cbb-b05a-e74befafaf37")]
        public readonly InputSlot<Texture2D> Image = new InputSlot<Texture2D>();

        [Input(Guid = "b25d483f-1fdf-4d76-974c-8e781a405914")]
        public readonly InputSlot<bool> Enable = new InputSlot<bool>();

    }
}

