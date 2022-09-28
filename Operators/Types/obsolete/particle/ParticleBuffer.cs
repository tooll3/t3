using System.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_3546ec16_cf88_4915_b8bb_9566769687ca
{
    public class ParticleBuffer : Instance<ParticleBuffer>
    {
        [Output(Guid = "FECD0B22-F28E-4EF9-80E4-76ED1EFE973C")]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public ParticleBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            int count = Count.GetValue(context);
            int stride = 48;
            var bufferData = Enumerable.Repeat(-10.0f, count * (stride / 4)).ToArray(); // init with negative lifetime other values doesn't matter
            ResourceManager.Instance().SetupStructuredBuffer(bufferData, stride * count, stride, ref Buffer.Value);

            var symbolChild = Parent.Symbol.Children.Single(c => c.Id == SymbolChildId);
            Buffer.Value.DebugName = symbolChild.ReadableName;
            Log.Info($"{symbolChild.ReadableName} updated");
        }

        [Input(Guid = "61D1BE34-26CF-43DB-9219-7A97AB3113B8")]
        public readonly InputSlot<int> Count = new InputSlot<int>(1000);
    }
}