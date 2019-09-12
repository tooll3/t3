using System.Runtime.InteropServices;
using T3.Core;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types
{
    public class ParamConstBuffer : Instance<ParamConstBuffer>
    {
        [Output(Guid = "{89957A76-09F1-4448-B23E-39DFDD0AA5B0}")]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        private uint _bufferResId;

        public ParamConstBuffer()
        {
            Buffer.UpdateAction = Update;
            Buffer.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        private void Update(EvaluationContext context)
        {
            var bufferContent = new ParamBufferLayout(Param1.GetValue(context), Param2.GetValue(context), Param3.GetValue(context), Param4.GetValue(context));
            ResourceManager.Instance().SetupConstBufferForCS(bufferContent, ref Buffer.Value, 0);
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct ParamBufferLayout
        {
            public ParamBufferLayout(float param1, float param2, float param3, float param4)
            {
                Param1 = param1;
                Param2 = param2;
                Param3 = param3;
                Param4 = param4;
            }

            [FieldOffset(0)]
            public float Param1;
            [FieldOffset(4)]
            public float Param2;
            [FieldOffset(8)]
            public float Param3;
            [FieldOffset(12)]
            public float Param4;
        }       

        [Input(Guid = "{3BB4BAB9-96CD-4E08-BAB7-2A15FB4F11C0}")]
        public readonly InputSlot<float> Param1 = new InputSlot<float>();
        [Input(Guid = "{A3E13E00-835D-4857-B804-A1E300EBCA2A}")]
        public readonly InputSlot<float> Param2 = new InputSlot<float>();
        [Input(Guid = "{E3F6E24D-97D7-4679-8A4C-C7ADA2E01022}")]
        public readonly InputSlot<float> Param3 = new InputSlot<float>();
        [Input(Guid = "{B2208D3A-F321-4F4B-81DE-6EB3026FE39C}")]
        public readonly InputSlot<float> Param4 = new InputSlot<float>();
    }
}