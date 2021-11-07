using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d7ca8763_ad6e_42dd_bb02_ee0a0abb2565
{
    public class ListAppender : Instance<ListAppender>
    {
        [Output(Guid = "b435c25e-358e-4c89-b48e-feace1049476")]
        public readonly Slot<System.Collections.Generic.List<string>> OutputList = new Slot<System.Collections.Generic.List<string>>();

        public ListAppender()
        {
            OutputList.UpdateAction = Update;
            OutputList.Value = new List<string>();
            Input.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        public void Update(EvaluationContext context)
        {
            var count = Count.GetValue(context);
            OutputList.Value.Clear();
            OutputList.Value.Capacity = count;
            
            for (int i = 0; i < count; i++)
            {
                DirtyFlag.InvalidationRefFrame++;
                Input.Invalidate();
                OutputList.Value.Add(Input.GetValue(context));
            }
        }

        [Input(Guid = "8a14713b-e625-4539-90f5-99a9c1963399")]
        public readonly InputSlot<string> Input = new InputSlot<string>();

        [Input(Guid = "869b79a2-40d0-42a3-a0a7-b00579888543")]
        public readonly InputSlot<int> Count = new InputSlot<int>();
    }
}