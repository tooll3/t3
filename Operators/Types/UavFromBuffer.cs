using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cc4847f8_a8a3_4da5_8b71_c4f3a3f488e6
{
    public class UavFromBuffer : Instance<UavFromBuffer>
    {
        [Output(Guid = "D7CF0DAE-FFB7-4408-A1EA-B0C1B4BC60C2")]
        public readonly Slot<UnorderedAccessView> UnorderedAccessView = new Slot<UnorderedAccessView>();

        public UavFromBuffer()
        {
            UnorderedAccessView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var buffer = Buffer.GetValue(context);
            ResourceManager.Instance().CreateBufferUav<uint>(buffer, Format.R32_UInt, ref UnorderedAccessView.Value);
        }

        [Input(Guid = "58EBAE6E-7D8C-45A0-8266-8B71F601DA0A")]
        public readonly InputSlot<SharpDX.Direct3D11.Buffer> Buffer = new InputSlot<SharpDX.Direct3D11.Buffer>();
    }
}