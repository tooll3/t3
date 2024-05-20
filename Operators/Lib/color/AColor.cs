using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_70176d3c_825c_40b3_8121_a465735518fe
{
    public class AColor : Instance<AColor>
    {
        [Output(Guid = "fae78369-9db9-4b00-94f2-89e7581db426")]
        public readonly Slot<Vector4> Output = new();

        public AColor()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Output.Value = RGBA.GetValue(context);
        }
        
        [Input(Guid = "03dc1ef1-d75a-4f65-a607-d5dc4de56a2c")]
        public readonly InputSlot<Vector4> RGBA = new();
        

    }
}