using System.Runtime.InteropServices;
using SharpDX;
using T3.Core.DataStructures;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_a60adc26_d7c6_4615_af78_8d2d6da46b79
{
    public class TransformsConstBuffer : Instance<TransformsConstBuffer>
    {
        [Output(Guid = "7A76D147-4B8E-48CF-AA3E-AAC3AA90E888", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public TransformsConstBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var bufferContent = new TransformBufferLayout(context);
            ResourceManager.SetupConstBuffer(bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName = nameof(TransformsConstBuffer);
        }
    }
}