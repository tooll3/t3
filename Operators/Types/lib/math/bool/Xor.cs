using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0d4f4e07_5cb2_4d80_bf8e_3deadf968724 
{
    public class Xor : Instance<Xor>
    {
        [Output(Guid = "8b34d471-3688-4109-aad7-4e76811ed26e")]
        public readonly Slot<bool> Result = new();

        public Xor()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var a = A.GetValue(context);
            Result.Value = B.GetValue(context) ? !a : a;
        }
        
        [Input(Guid = "efce4921-6e52-4eca-982e-ddd8d2e8f181")]
        public readonly InputSlot<bool> A = new();

        [Input(Guid = "cc0fdde0-a5a9-4fbd-b85b-ddfba37a85ac")]
        public readonly InputSlot<bool> B = new();
    }
}
