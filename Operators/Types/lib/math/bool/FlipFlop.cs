using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

// ReSharper disable InconsistentNaming

namespace T3.Operators.Types.Id_ec1ba1fb_ceb7_4d4f_86b4_389589c7e6f0
{
    public class FlipFlop : Instance<FlipFlop>
    {
        [Output(Guid = "c49bcf90-dee5-4132-b791-27f023be5d93")]
        public readonly Slot<bool> Result = new();

        public FlipFlop()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var isTriggered = Trigger.GetValue(context);
            var isReset = ResetTrigger.GetValue(context);
            var defaultValue = DefaultValue.GetValue(context);

            if (isReset)
            {
                // Log.Debug(" exit default " + defaultValue, this);
                Result.Value = defaultValue;
            }
            else if (isTriggered)
            {
                // Log.Debug(" exit true", this);
                Result.Value = true;
            }
        }

        [Input(Guid = "44678eb5-8320-4ac9-b757-e0f6f94f5274")]
        public readonly InputSlot<bool> Trigger = new();

        [Input(Guid = "D386F323-DE9A-4FF5-9769-2C4E01D98D06")]
        public readonly InputSlot<bool> ResetTrigger = new();

        [Input(Guid = "1E8C11A9-D0FC-4016-9428-BED7FBD0BF68")]
        public readonly InputSlot<bool> DefaultValue = new();
    }
}