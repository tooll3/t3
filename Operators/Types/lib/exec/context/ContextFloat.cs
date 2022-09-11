using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e6072ecf_30d2_4c52_afa1_3b195d61617b
{
    public class ContextFloat : Instance<ContextFloat>
    {
        [Output(Guid = "e368ba33-827e-4e08-aa19-ba894b40906a")]
        public readonly Slot<float> Result = new Slot<float>();

        public ContextFloat()
        {
            Result.UpdateAction = Update;
            Variable.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
        }

        private void Update(EvaluationContext context)
        {
            string variableName = Variable.GetValue(context);
            if (context.FloatVariables.TryGetValue(variableName, out float value))
            {
                // Log.Debug($"{variableName} : {value}");
                Result.Value = value;
            }
        }

        [Input(Guid = "015d1ea0-ea51-4038-893a-4af2f8584631")]
        public readonly InputSlot<string> Variable = new InputSlot<string>();
    }
}

