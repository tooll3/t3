using System;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0c2dcf1f_48ff_45aa_a28b_c5900279ce6e
{
    public class Int3FromInts : Instance<Int3FromInts>
    {
        [Output(Guid = "9D9CAFA3-FDA3-48D9-81D5-5374FC439688")]
        public readonly Slot<Int3> Result = new Slot<Int3>();

        public Int3FromInts()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = new Int3(X.GetValue(context), Y.GetValue(context), Z.GetValue(context));
        }
        
        [Input(Guid = "A1B1EA49-4234-4317-A83C-5537273EEC4E")]
        public readonly InputSlot<int> X = new InputSlot<int>();

        [Input(Guid = "5775E14F-7DF4-4F2E-8712-D2A6E3C10ECF")]
        public readonly InputSlot<int> Y = new InputSlot<int>();
        
        [Input(Guid = "0564CF1C-40A3-40FC-92DD-2E2632EEC37C")]
        public readonly InputSlot<int> Z = new InputSlot<int>();
    }
}
