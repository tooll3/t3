using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cd43942a_887e_4e34_bc54_0c2e5e8bc2af
{
    public class BoolToInt : Instance<BoolToInt>
    {
        [Output(Guid = "b0cfa6f9-3c3d-4499-b21a-5904d1cb3bd7")]
        public readonly Slot<int> Result = new Slot<int>();
        
        public BoolToInt()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = BoolValue.GetValue(context) 
                               ? ResultForTrue.GetValue(context)
                               : ResultForFalse.GetValue(context);
            ResultForTrue.DirtyFlag.Clear();
            ResultForFalse.DirtyFlag.Clear();
        }
        
        [Input(Guid = "c644165f-3901-4dbf-8091-05f958e668e5")]
        public readonly InputSlot<bool> BoolValue = new InputSlot<bool>();
        
        [Input(Guid = "9B64F287-D14A-493E-A1C7-DCBCDC703849")]
        public readonly InputSlot<int> ResultForFalse = new InputSlot<int>();
        
        [Input(Guid = "CBBB6B8A-0DC9-4A85-8ABC-E4C9C1C9C8BE")]
        public readonly InputSlot<int> ResultForTrue = new InputSlot<int>();


    }
}