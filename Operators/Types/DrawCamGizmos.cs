using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cdf5dd6a_73dc_4779_a366_df19b69071a6
{
    public class DrawCamGizmos : Instance<DrawCamGizmos>
    {
        [Output(Guid = "6cee53fc-92df-4a9e-b519-da857bdf9419")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "fce4c7b7-473a-4f55-9b26-83e562462b3b")]
        public readonly InputSlot<int> IntValue = new InputSlot<int>();


    }
}

