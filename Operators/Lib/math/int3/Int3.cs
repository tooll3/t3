using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.math.int3
{
	[Guid("0c2dcf1f-48ff-45aa-a28b-c5900279ce6e")]
    public class Int3 : Instance<Int3>
    {
        [Output(Guid = "9D9CAFA3-FDA3-48D9-81D5-5374FC439688")]
        public readonly Slot<T3.Core.DataTypes.Vector.Int3> Result = new ();

        public Int3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = new T3.Core.DataTypes.Vector.Int3(X.GetValue(context), Y.GetValue(context), Z.GetValue(context));
        }
        
        [Input(Guid = "A1B1EA49-4234-4317-A83C-5537273EEC4E")]
        public readonly InputSlot<int> X = new();

        [Input(Guid = "5775E14F-7DF4-4F2E-8712-D2A6E3C10ECF")]
        public readonly InputSlot<int> Y = new();
        
        [Input(Guid = "0564CF1C-40A3-40FC-92DD-2E2632EEC37C")]
        public readonly InputSlot<int> Z = new();
    }
}
