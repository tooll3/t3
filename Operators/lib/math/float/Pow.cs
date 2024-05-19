using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_58aa74af_32aa_4c46_8bb5_5811f16bf7f8 
{
    public class Pow : Instance<Pow>
    {
        [Output(Guid = "f858c25a-0099-40ec-93dc-dd929c8774f0")]
        public readonly Slot<float> Result = new();

        public Pow()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var pow = Exponent.GetValue(context);
            Result.Value = (float)Math.Pow(v,pow);
        }
        
        [Input(Guid = "376ad938-fe23-4f40-901a-b1b582ea4904")]
        public readonly InputSlot<float> Value = new();

        [Input(Guid = "36853585-1A17-47F7-8485-569F17F48C66")]
        public readonly InputSlot<float> Exponent = new();
    }
}
