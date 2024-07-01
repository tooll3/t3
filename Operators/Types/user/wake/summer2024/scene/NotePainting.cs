using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_15566742_6e21_4ebb_bb35_fa1949fa4e4a
{
    public class NotePainting : Instance<NotePainting>
    {
        [Output(Guid = "a6934560-7a8d-434d-9a8a-e030be9f622b")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


        [Input(Guid = "cd0ca705-10b3-4156-ab69-20071a6e4e60")]
        public readonly InputSlot<bool> TriggerIncrement = new InputSlot<bool>();

        [Input(Guid = "4d20fc8b-8916-45ef-a6ac-9880fb1fca69")]
        public readonly InputSlot<bool> Trigger = new InputSlot<bool>();

    }
}

