using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_6c1271a0_058f_4ff0_940b_f196e5debdf7
{
    public class GetForegroundColor : Instance<GetForegroundColor>
    {
        [Output(Guid = "F962854B-00D6-4EB3-AA4C-E5D4BD500672", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Vector4> Result = new();

        public GetForegroundColor()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = context.ForegroundColor;
        }
    }
}

