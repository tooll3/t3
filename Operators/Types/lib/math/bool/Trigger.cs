using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0bec016a_5e1b_467a_8273_368d4d6b9935
{
    public class Trigger : Instance<Trigger>
    {
        [Output(Guid = "2451ea62-9915-4ec1-a65e-4d44a3758fa8")]
        public readonly Slot<bool> Result = new();

        public Trigger()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Log.Debug("Update Trigger",this);
            Result.Value = BoolValue.GetValue(context);
            if (Result.Value)
            {
                SetTriggered(false);                
            }
        }

        public void Activate()
        {
            SetTriggered(true);
        }

        private void SetTriggered(bool state)
        {
            BoolValue.TypedInputValue.Value = state;
            BoolValue.Input.IsDefault = false;
            BoolValue.DirtyFlag.Invalidate();
        }

        
        [Input(Guid = "E7C1F0AF-DA6D-4E33-AC86-7DC96BFE7EB3")]
        public readonly InputSlot<bool> BoolValue = new();
    }
}
