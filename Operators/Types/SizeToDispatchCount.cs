using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cc11774e_82dd_409f_97fb_5be3f2746f9d
{
    public class SizeToDispatchCount : Instance<SizeToDispatchCount>
    {
        [Output(Guid = "3b0f7d82-3254-4b4d-baea-bc9aa003768a")]
        public readonly Slot<SharpDX.Int3> DispatchCount = new Slot<SharpDX.Int3>();

        public SizeToDispatchCount()
        {
            DispatchCount.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Size2 size = Size.GetValue(context);
            Int3 threadGroups = ThreadGroups.GetValue(context);
            if (threadGroups.X == 0 || threadGroups.Y == 0)
                return;

            DispatchCount.Value = new Int3(size.Width / threadGroups.X, 
                                           size.Height / threadGroups.Y, 1);
        }

        [Input(Guid = "714e7c0d-0137-4bc6-9e5b-93386b2efe13")]
        public readonly InputSlot<Size2> Size = new InputSlot<Size2>();

        [Input(Guid = "71fe6847-b8e3-4cc7-895c-b10db0136e1c")]
        public readonly InputSlot<SharpDX.Int3> ThreadGroups = new InputSlot<SharpDX.Int3>();
    }
}