using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dcbf2e26_7870_4ccf_9262_9dda256a610a
{
    public class RenderImageFx : Instance<RenderImageFx>
    {
        [Output(Guid = "511d880c-8634-4825-a106-41768114fd00")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "0d74afd7-cc7d-4b15-a994-3b0a272e0381")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "3015755b-5ded-4e22-b621-4b0ad28e85cb")]
        public readonly MultiInputSlot<float> Params = new MultiInputSlot<float>();

        [Input(Guid = "2eb7f415-8e87-4237-b399-383e4f73c154")]
        public readonly InputSlot<string> Source2 = new InputSlot<string>();

        [Input(Guid = "5e5a5bcb-275e-4b25-9442-07d79a545d53")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();

    }
}

