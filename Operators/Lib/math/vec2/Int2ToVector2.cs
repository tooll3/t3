using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d1a66374_f0e8_4ef5_adf4_2871ec549d2a
{
    public class Int2ToVector2 : Instance<Int2ToVector2>
    {
        [Output(Guid = "ea84de7c-7381-4689-90c2-586308d3b15d")]
        public readonly Slot<System.Numerics.Vector2> Result = new();

        
        public Int2ToVector2()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var s = Int2.GetValue(context);
            Result.Value = s;
        }
        
        [Input(Guid = "5C493D16-CC75-4CD5-96D9-ECA3ADEACCD9")]
        public readonly InputSlot<Int2> Int2 = new();
        
        
    }
}
