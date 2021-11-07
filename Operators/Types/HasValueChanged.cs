using System;
using System.Diagnostics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_146fae64_18da_4183_9794_a322f47c669e
{
    public class HasValueChanged : Instance<HasValueChanged>
    {
        [Output(Guid = "35ab8188-77a1-4cd9-b2ad-c503034e49f9")]
        public readonly Slot<bool> HasIncreased = new Slot<bool>();

        [Output(Guid = "ab818835-77a1-4cd9-b2ad-c503034e49f9")]
        public readonly Slot<float> Delta = new Slot<float>();


        public HasValueChanged()
        {
            HasIncreased.UpdateAction = Update;
            Delta.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var newValue = Value.GetValue(context);
            var threshold = Threshold.GetValue(context);

            var hasChanged = false;

            switch ((Modes)Mode.GetValue(context))
            {
                case Modes.Changed:
                    var increase = Math.Abs(newValue - _lastValue) >= threshold;
                    hasChanged = increase;

                    break;
                case Modes.Increased:
                    hasChanged = newValue > _lastValue + threshold;
                    break;
                case Modes.Decreased:
                    hasChanged = newValue < _lastValue - threshold;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            HasIncreased.Value = hasChanged;

            Delta.Value = newValue - _lastValue;
            _lastValue = newValue;
        }

        private float _lastValue;

        [Input(Guid = "7f5fb125-8aca-4344-8b30-e7d4e7873c1c")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        [Input(Guid = "1e03f11b-f6cc-497e-9528-ed9490e878b5")]
        public readonly InputSlot<float> Threshold = new InputSlot<float>();

        [Input(Guid = "031ef11b-f6cc-497e-9528-ed9490e878b5", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        private enum Modes
        {
            Changed,
            Increased,
            Decreased
        }


    }
}