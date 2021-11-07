using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_81ff4731_e244_4995_b03d_5544d9211d83
{
    public class CopyStructureCount : Instance<CopyStructureCount>
    {
        [Output(Guid = "1C8785E7-A709-4D8C-91CB-A10C052A6169", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        public CopyStructureCount()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            var deviceContext = device.ImmediateContext;

            var targetBuffer = TargetBuffer.GetValue(context);
            var sourceBufferUav = SourceBuffer.GetValue(context);
            if (targetBuffer == null || sourceBufferUav == null)
                return;

            deviceContext.CopyStructureCount(targetBuffer, DstAlignedByteOffset.GetValue(context), sourceBufferUav);
        }
        
        [Input(Guid = "3AC041C8-2C75-425A-9935-ED6DB5DA5CD2")]
        public readonly InputSlot<UnorderedAccessView> SourceBuffer = new InputSlot<UnorderedAccessView>();
        [Input(Guid = "1386A5E3-75E4-4421-A35B-1D5F79B2CD32")]
        public readonly InputSlot<Buffer> TargetBuffer = new InputSlot<Buffer>();
        [Input(Guid = "17ECE64E-FF76-41F5-BF36-762A5BAAB2AF")]
        public readonly InputSlot<int> DstAlignedByteOffset = new InputSlot<int>();
    }
}