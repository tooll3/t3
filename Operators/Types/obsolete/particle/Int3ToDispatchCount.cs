using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_16e4254d_f9be_421b_bb43_d5bae18ccb12
{
    public class Int3ToDispatchCount : Instance<Int3ToDispatchCount>
    {
        [Output(Guid = "3b7c2a16-9fbe-469f-99f5-f2bde57e16b1")]
        public readonly Slot<SharpDX.Int3> DispatchCount = new Slot<SharpDX.Int3>();

        public Int3ToDispatchCount()
        {
            DispatchCount.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var size = Size.GetValue(context);
            Int3 threadGroups = ThreadGroups.GetValue(context);
            if (threadGroups.X == 0 || threadGroups.Y == 0)
                return;

            DispatchCount.Value = new Int3(size.X / threadGroups.X, 
                                           size.Y / threadGroups.Y, 
                                           size.Z / threadGroups.Z);
        }

        [Input(Guid = "3749863B-A703-44CF-B78F-A92CF3590422")]
        public readonly InputSlot<Int3> Size = new InputSlot<Int3>();

        [Input(Guid = "ed385d57-5c79-4109-826d-e82ab76214d3")]
        public readonly InputSlot<SharpDX.Int3> ThreadGroups = new InputSlot<SharpDX.Int3>();
    }
}