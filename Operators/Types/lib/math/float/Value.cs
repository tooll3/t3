using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe
{
    public class Value : Instance<Value>
    {
        [Output(Guid = "f83f1835-477e-4bb6-93f0-14bf273b8e94")]
        public readonly Slot<float> Result = new Slot<float>();

        public Value()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Float.GetValue(context);
        }
        
        [Input(Guid = "7773837e-104a-4b3d-a41f-cadbd9249af2")]
        public readonly InputSlot<float> Float = new();
        
        [Input(Guid = "90045A45-F549-4CB5-B57C-C39A0664E84F")]
        public readonly InputSlot<float> SliderMin = new();
        
        [Input(Guid = "1785EABD-54FA-4B8C-AE5A-54112C2E50B8")]
        public readonly InputSlot<float> SliderMax = new();

        [Input(Guid = "97EAECBB-9290-4D23-932D-DC183EBFCDA6")]
        public readonly InputSlot<bool> ClampSlider = new();


    }
}
