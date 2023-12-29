using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_24b56330_b9c5_4454_a398_0500b0422ce1 
{
    public class Sqrt : Instance<Sqrt>
    {
        [Output(Guid = "915a7042-4bdc-4238-a59e-04eed3020f12")]
        public readonly Slot<float> Result = new();

        public Sqrt()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            Result.Value = MathF.Sqrt(v);
        }
        
        [Input(Guid = "a91ef6c9-4d5d-4fb5-9e83-c2f4fbb9769f")]
        public readonly InputSlot<float> Value = new();
    }
}
