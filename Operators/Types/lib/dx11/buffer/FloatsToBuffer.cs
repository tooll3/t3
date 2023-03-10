using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;
using Utilities = T3.Core.Utils.Utilities;
using Vector4 = SharpDX.Vector4;

namespace T3.Operators.Types.Id_724da755_2d0c_42ab_8335_8c88ec5fb078
{
    public class FloatsToBuffer : Instance<FloatsToBuffer>
    {
        [Output(Guid = "f5531ffb-dbde-45d3-af2a-bd90bcbf3710")]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public FloatsToBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var param = Params.GetCollectedTypedInputs();
            var vec4params = Vec4Params.GetValue(context);

            var floatParamCount = param.Count;
            var vec4ArrayLength = vec4params?.Length ?? 0;

            var totalFloatCount = floatParamCount + vec4ArrayLength * 4;

            int arraySize = (totalFloatCount / 4 + (totalFloatCount % 4 == 0 ? 0 : 1)) * 4; // always 16byte slices for alignment
            var array = new float[arraySize];

            if (array.Length == 0)
                return;

            // Add Vec4 Arrays first
            if (vec4params != null)
            {
                for (var vec4Index = 0; vec4Index < vec4ArrayLength; vec4Index++)
                {
                    var vec4 = vec4params[vec4Index];
                    array[vec4Index * 4 + 0] = vec4[0];
                    array[vec4Index * 4 + 1] = vec4[1];
                    array[vec4Index * 4 + 2] = vec4[2];
                    array[vec4Index * 4 + 3] = vec4[3];
                }
            }

            // Add Floats
            for (var floatIndex = 0; floatIndex < floatParamCount; floatIndex++)
            {
                array[floatIndex + vec4ArrayLength * 4] = param[floatIndex].GetValue(context);
            }

            Params.DirtyFlag.Clear();

            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;

            var size = sizeof(float) * array.Length;
            using (var data = new DataStream(size, true, true))
            {
                data.WriteRange(array);
                data.Position = 0;

                if (Buffer.Value == null || Buffer.Value.Description.SizeInBytes != size)
                {
                    Utilities.Dispose(ref Buffer.Value);
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = size,
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    Buffer.Value = new Buffer(device, data, bufferDesc);
                }
                else
                {
                    device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), Buffer.Value, 0);
                }
            }

            Buffer.Value.DebugName = nameof(FloatsToBuffer);
        }
        
        [Input(Guid = "914EA6E8-ABC6-4294-B895-8BFBE5AFEA0E")]
        public readonly InputSlot<SharpDX.Vector4[]> Vec4Params = new();

        [Input(Guid = "49556D12-4CD1-4341-B9D8-C356668D296C")]
        public readonly MultiInputSlot<float> Params = new MultiInputSlot<float>();

    }
}