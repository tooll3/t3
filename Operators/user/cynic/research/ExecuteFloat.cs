using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_35dd9888_dea5_4ca4_8e1d_b8f0a59ec0ea
{
    public class ExecuteFloat : Instance<ExecuteFloat>
    {
        [Output(Guid = "30fe17a0-1825-47f3-806d-c3d74a75d691", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        public ExecuteFloat()
        {
            Output.UpdateAction = Update;
        }

        public void Update(EvaluationContext context)
        {
            Distance.GetValue(context);
        }

        [Input(Guid = "baf213d5-a441-4bcd-a261-c873cb8353cb")]
        public readonly InputSlot<float> Distance = new();

    }
}

