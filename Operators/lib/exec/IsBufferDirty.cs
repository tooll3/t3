using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_02b84d90_aa4e_4cf9_94d5_feb5d7ca731e
{
    public class IsBufferDirty : Instance<IsBufferDirty>
    {
        [Output(Guid = "b3f19e80-8c09-4e38-9b6d-fc864facd34a")]
        public readonly Slot<bool> HasChanged = new();
        

        public IsBufferDirty()
        {
            HasChanged.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var isDirty = InputBuffer.DirtyFlag.IsDirty;
            HasChanged.Value = isDirty;
            if (isDirty)
            {
                InputBuffer.GetValue(context);
            }
        }
        
        [Input(Guid = "CCB0BE73-78CA-4094-AAF3-2E487631A947")]
        public readonly InputSlot<BufferWithViews> InputBuffer = new();

    }
}