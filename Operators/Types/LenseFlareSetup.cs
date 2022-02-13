using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_eb4d5014_2619_4368_a1b8_521db8372243
{
    public class LenseFlareSetup : Instance<LenseFlareSetup>
    {
        [Output(Guid = "27af7a2d-8bef-413f-9b41-381a3c9022de")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "6485746d-1f22-4c39-bbc5-773e29feb4c0")]
        public readonly InputSlot<float> Brightness = new InputSlot<float>();

        [Input(Guid = "5dd9abed-da33-4f80-ba83-a817d2105add")]
        public readonly InputSlot<int> RandomSeed = new InputSlot<int>();


    }
}

