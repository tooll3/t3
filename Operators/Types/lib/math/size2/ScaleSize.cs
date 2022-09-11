using System;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c6d50423_54ea_4c9d_b547_eb78cc2c950c
{
    public class ScaleSize : Instance<ScaleSize>
    {
        [Output(Guid = "c2c27def-70f2-4f07-9796-11b62e5329e2")]
        public readonly Slot<Size2> Result = new Slot<Size2>();
        
        
        public ScaleSize()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var source = InputSize.GetValue(context);
            var factor = Factor.GetValue(context);
            
            Result.Value = new Size2((int)(source.Width * factor), (int)(source.Height * factor));
        }
        
        [Input(Guid = "DDCEB7DF-1C6F-4545-9669-B1B4A80E75E8")]
        public readonly InputSlot<Size2> InputSize = new InputSlot<Size2>();
        
        [Input(Guid = "133BBC5A-BDBF-4993-BD1A-878EC93EE04F")]
        public readonly InputSlot<float> Factor = new InputSlot<float>();
        
    }
}
