using System;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5f734c25_9f1a_436c_b56c_7e0a1e07fdda 
{
    public class ClampInt : Instance<ClampInt>
    {
        [Output(Guid = "E6AAE72F-8C22-4133-BA0D-C3635751D715")]
        public readonly Slot<int> Result = new Slot<int>();

        public ClampInt()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var min = Min.GetValue(context);
            var max = Max.GetValue(context);
            Result.Value = MathUtil.Clamp(v, min, max);
        }
        
        [Input(Guid = "75A09454-6CDE-458B-9314-05A99B2E5919")]
        public readonly InputSlot<int> Value = new InputSlot<int>();

        [Input(Guid = "E715919D-F3E3-4708-90A6-B55EFB379257")]
        public readonly InputSlot<int> Min = new InputSlot<int>();
        
        [Input(Guid = "23E55B5D-B469-4D0F-A495-7E87FE65CCCF")]
        public readonly InputSlot<int> Max = new InputSlot<int>();

        
    }
}
