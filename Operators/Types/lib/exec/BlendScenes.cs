using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_6e67d136_9285_48c6_a557_0a07361fc847
{
    public class BlendScenes : Instance<BlendScenes>
    {
        [Output(Guid = "bb6e9504-2e2c-413a-a455-5dd4ef41e3cb")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "998b7cbc-b54c-4e81-bc61-31a7e05ce8e0")]
        public readonly MultiInputSlot<Command> Scenes = new MultiInputSlot<Command>();

        [Input(Guid = "ce0e7890-b25c-45bd-8d7f-20865cc0a51a")]
        public readonly InputSlot<float> BlendFraction = new InputSlot<float>();


    }
}

