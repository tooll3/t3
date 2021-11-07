using System;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_c513c58d_e45c_408d_a0b8_250c9af31545;

namespace T3.Operators.Types.Id_ef3a1411_e88c_43a8_83b4_931fdbf16c75
{
    public class DampPeakDecay : Instance<DampPeakDecay>
    {
        [Output(Guid = "A60A2E7B-99B7-489A-A662-301A6E71A885", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();
        
        public DampPeakDecay()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var runTime = EvaluationContext.GlobalTimeForEffects;
            
            var wasEvaluatedThisFrame = runTime <= _lastEvalTime;
            if (wasEvaluatedThisFrame)
            {
                Value.DirtyFlag.Clear();
                Decay.DirtyFlag.Clear();
                return;
            }

            _lastEvalTime = runTime;

            var value = Value.GetValue(context);
            _dampedValue = _dampedValue > value
                               ? MathUtils.Lerp(_dampedValue, value, Decay.GetValue(context))
                               : value;

            Result.Value = _dampedValue;
        }

        private float _dampedValue; 

        private double _lastEvalTime;
        
        
        [Input(Guid = "d548e650-ca2b-4a24-bca0-16c846fa9224")]
        public readonly InputSlot<float> Value = new InputSlot<float>();


        [Input(Guid = "A2B624B4-ED36-45EC-A901-EFE6D45AA067")]
        public readonly InputSlot<float> Decay = new InputSlot<float>(0.05f);
    }
}