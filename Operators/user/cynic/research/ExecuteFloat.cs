using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.cynic.research
{
	[Guid("35dd9888-dea5-4ca4-8e1d-b8f0a59ec0ea")]
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

