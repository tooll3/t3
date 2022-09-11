using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a9784e5e_7696_49a0_bb77_2302587ede59
{
    public class PickString : Instance<PickString>
    {
        [Output(Guid = "74104EB6-DFC2-4AD2-9600-91C5A33855D4")]
        public readonly Slot<string> Selected = new Slot<string>();

        public PickString()
        {
            Selected.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var connections = Input.GetCollectedTypedInputs();
            if (connections == null || connections.Count == 0)
                return;

            var index = Index.GetValue(context);
            if (index < 0)
                index = -index;

            index %= connections.Count;
            Selected.Value = connections[index].GetValue(context);
        }

        [Input(Guid = "202CE6D5-EE5A-41C7-BD04-4C1490F3EA9C")]
        public readonly MultiInputSlot<string> Input = new MultiInputSlot<string>();

        [Input(Guid = "20E76577-92EE-443D-9630-EBC41E38BB85")]
        public readonly InputSlot<int> Index = new InputSlot<int>(0);
    }
}