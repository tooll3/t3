namespace T3.Core.Operator.Types
{
    public class Random : Instance<Random>
    {
        [Output]
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

        [IntInput(DefaultValue = 3)]
        public readonly InputSlot<int> Seed = new InputSlot<int>();
    }
}