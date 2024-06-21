using System.Runtime.InteropServices;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.exec.context
{
	[Guid("6c1271a0-058f-4ff0-940b-f196e5debdf7")]
    public class GetForegroundColor : Instance<GetForegroundColor>
    {
        [Output(Guid = "F962854B-00D6-4EB3-AA4C-E5D4BD500672", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Vector4> Result = new();

        public GetForegroundColor()
        {
            Result.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = context.ForegroundColor;
        }
    }
}

