using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_acdd78b1_4e66_4fd0_a36b_5318670fefd4
{
    public class ToUpperCase : Instance<ToUpperCase>
    {
        [Output(Guid = "ecf66a1e-45e5-4e0c-ac9e-a784a9339153")]
        public readonly Slot<string> Result = new Slot<string>();

        public ToUpperCase()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var str = Input2.GetValue(context);
            Result.Value = str?.ToUpper();
        }

        [Input(Guid = "041C98B6-4450-46D7-9DAE-C9030C88B9E6")]
        public readonly InputSlot<string> Input2 = new InputSlot<string>();
    }
}