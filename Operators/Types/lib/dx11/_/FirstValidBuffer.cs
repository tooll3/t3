using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b4a8a055_6ae3_4b56_8b65_1b7b5f87d19a
{
    public class FirstValidBuffer : Instance<FirstValidBuffer>
    {
        [Output(Guid = "bf3a690e-8611-470c-aad0-8099908e63c8")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();
        
        
        public FirstValidBuffer()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var connections = Input.GetCollectedTypedInputs();
            if (connections == null || connections.Count == 0)
                return;

            for (int index = 0; index < connections.Count; index++)
            {
                var v =  connections[index].GetValue(context);
                if (v != null)
                {
                    Output.Value = v;
                    break;
                }
            }
        }        
        
        [Input(Guid = "73cf2380-b592-4c63-9e62-70411e4f3ad5")]
        public readonly MultiInputSlot<BufferWithViews> Input = new();
    }
}

