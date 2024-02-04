using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib.math.@float
{
	[Guid("10673c38-8c7e-4aa1-8dcd-3f2711c709b5")]
    public class Random : Instance<Random>
    {
        [Output(Guid = "{DFB39F6E-7B1C-41F3-9F31-B71CAEE629F9}")]
        public readonly Slot<float> Result = new();

        public Random()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var random = new System.Random(Seed.GetValue(context));
            var firstIsGarbage = (float)random.NextDouble();
            Result.Value = (float)MathUtils.RemapAndClamp((double)(float)random.NextDouble(), 0f,1f,Min.GetValue(context), Max.GetValue(context));
        }

        [Input(Guid = "{F2513EAD-7022-4774-8767-7F33D1B92B26}")]
        public readonly InputSlot<int> Seed = new();
        
        [Input(Guid = "48762E06-8377-464B-8FB9-C7D3B51C3F8E")]
        public readonly InputSlot<float> Min = new();

        [Input(Guid = "5755454F-98FE-49EF-9611-A7C3750C4F9A")]
        public readonly InputSlot<float> Max = new();

    }
}