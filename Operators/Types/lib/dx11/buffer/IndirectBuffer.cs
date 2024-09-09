using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_5a88fa27_16ad_454f_a08d_5e65dd75cefe
{
    public class IndirectBuffer : Instance<IndirectBuffer>
    {
        [Output(Guid = "837133D3-308C-48AA-9AFE-B9EB09E76A69")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new();


        public IndirectBuffer()
        {
            Buffer.UpdateAction = UpdateBuffer;
        }

        private void UpdateBuffer(EvaluationContext context)
        {
            int stride = Stride.GetValue(context);
            int sizeInBytes = stride * Count.GetValue(context);

            if (sizeInBytes <= 0)
                return;

            var bufferDesc = new BufferDescription
                             {
                                 Usage = ResourceUsage.Default,
                                 BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                 SizeInBytes = sizeInBytes,
                                 OptionFlags = ResourceOptionFlags.DrawIndirectArguments,
                                 StructureByteStride = stride
                             };
            ResourceManager.SetupBuffer(bufferDesc, ref Buffer.Value);
        }

        [Input(Guid = "38D649D2-861B-4302-A879-973D6405A4DE")]
        public readonly InputSlot<int> Stride = new();

        [Input(Guid = "70586A37-4B69-493E-BB47-98D7783DB16D")]
        public readonly InputSlot<int> Count = new();
    }
}