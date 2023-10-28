using System;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;


namespace T3.Operators.Types.Id_8e9a45db_a631_4c92_aea9_c252ea6e9708 
{
    public class MaxSize2 : Instance<MaxSize2>
    {
        [Output(Guid = "1D58BFF5-0FDF-4A42-ABF6-22FD8B74237F")]
        public readonly Slot<Size2> MaxSize = new();

        public MaxSize2()
        {
            
            MaxSize.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            int maxWidth =0 ;
            int maxHeight = 0;
            //Result.Value = 0;
            foreach (var input in Sizes.GetCollectedTypedInputs())
            {
                var s = input.GetValue(context);
                maxWidth = Math.Max(maxWidth, s.Width);
                maxHeight = Math.Max(maxHeight, s.Height);
            }
            Sizes.DirtyFlag.Clear();

            MaxSize.Value = new Size2(maxWidth, maxHeight);
        }
        
        
        [Input(Guid = "3FE2016D-C4BC-42E1-A3D8-F8BC34CFCF32")]
        public readonly MultiInputSlot<Size2> Sizes = new();
    }
}
