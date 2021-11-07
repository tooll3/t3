using System;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f1218934_f874_4f70_a077_0ebe7d12104d
{
    public class Int2ToSize2 : Instance<Int2ToSize2>
    {
        [Output(Guid = "3265FF5F-9D8D-48D5-A6F8-9085B4F19A78")]
        public readonly Slot<Size2> Result = new Slot<Size2>();

        [Output(Guid = "F5912E37-AF78-49C2-91D0-0675A8845F00")]
        public readonly Slot<int> Capacity = new Slot<int>();
        
        public Int2ToSize2()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var s = new Size2(X.GetValue(context), Y.GetValue(context));
            Result.Value = s;
            Capacity.Value = s.Width * s.Height;
        }
        
        [Input(Guid = "579E72D6-638E-4B17-BB4E-88A55E3A1D4D")]
        public readonly InputSlot<int> X = new InputSlot<int>();
        
        [Input(Guid = "53602AF2-48D9-42AB-80C3-AE1F1E600D28")]
        public readonly InputSlot<int> Y = new InputSlot<int>();
        
    }
}
