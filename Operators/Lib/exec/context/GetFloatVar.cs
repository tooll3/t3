using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.exec.context
{
	[Guid("e6072ecf-30d2-4c52-afa1-3b195d61617b")]
    public class GetFloatVar : Instance<GetFloatVar>
    {
        [Output(Guid = "e368ba33-827e-4e08-aa19-ba894b40906a", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        public GetFloatVar()
        {
            Result.UpdateAction += Update;
            Variable.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
        }

        private void Update(EvaluationContext context)
        {
            string variableName = Variable.GetValue(context);
            if (context.FloatVariables.TryGetValue(variableName, out float value))
            {
                // Log.Debug($"{variableName} : {value}", this);
                Result.Value = value;
            }
            else
            {
                Result.Value = FallbackDefault.GetValue(context);
            }
        }

        [Input(Guid = "015d1ea0-ea51-4038-893a-4af2f8584631")]
        public readonly InputSlot<string> Variable = new();
        
        [Input(Guid = "AE76829B-D17D-4443-9CF1-63E3C44B90C8")]
        public readonly InputSlot<float> FallbackDefault = new();
    }
}

