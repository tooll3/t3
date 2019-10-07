using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class ColorGrade : Instance<ColorGrade>
    {
        [Output(Guid = "{9832F278-2D9F-4B14-B92D-60ACB1C202CB}")]
        public readonly Slot<System.Numerics.Vector4> Output = new Slot<System.Numerics.Vector4>();

        public ColorGrade()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Output.Value = Color.GetValue(context);
        }

        [Input(Guid = "{8FFE42CF-6C2F-4D4E-8892-ADA31451D2B9}")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();
    }
}