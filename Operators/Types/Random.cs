using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class Random : Instance<Random>
    {
        [Output(Guid = "{DFB39F6E-7B1C-41F3-9F31-B71CAEE629F9}")]
        public readonly Slot<float> Result = new Slot<float>();

        public Random()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var random = new System.Random(Seed.GetValue(context));
            Result.Value = (float)random.NextDouble();
        }

        [Input(Guid = "{F2513EAD-7022-4774-8767-7F33D1B92B26}")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();
    }
}