using System.Linq;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0b5b14bf_c850_493a_afb1_72643926e214
{
    public class UavFromStructuredBuffer : Instance<UavFromStructuredBuffer>
    {
        [Output(Guid = "7C9A5943-3DEB-4400-BDB2-99F56DD1976C")]
        public readonly Slot<UnorderedAccessView> UnorderedAccessView = new Slot<UnorderedAccessView>();

        public UavFromStructuredBuffer()
        {
            UnorderedAccessView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var buffer = Buffer.GetValue(context);
            if (buffer == null)
                return;
            
            var bufferFlags = BufferFlags.GetValue(context);
            resourceManager.CreateStructuredBufferUav(buffer, bufferFlags, ref UnorderedAccessView.Value);
            if (UnorderedAccessView.Value == null)
                return;

            if (UnorderedAccessView.Value != null)
            {
                var symbolChild = Parent.Symbol.Children.Single(c => c.Id == SymbolChildId);
                UnorderedAccessView.Value.DebugName = symbolChild.ReadableName;
                // Log.Info($"{symbolChild.ReadableName} updated with ref {UnorderedAccessView.DirtyFlag.Reference}");
            }
        }

        [Input(Guid = "5d888f13-0ad8-4034-99ca-da36c8fb261c")]
        public readonly InputSlot<SharpDX.Direct3D11.Buffer> Buffer = new InputSlot<SharpDX.Direct3D11.Buffer>();

        [Input(Guid = "13B85721-7126-47BB-AB4F-096EAE59E412")]
        public readonly InputSlot<UnorderedAccessViewBufferFlags> BufferFlags = new InputSlot<UnorderedAccessViewBufferFlags>();
    }
}