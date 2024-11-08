using SharpDX;
using SharpDX.Direct3D11;
using Utilities = T3.Core.Utils.Utilities;

namespace Lib.dx11.buffer;

[Guid("3e587ede-f9ae-47d1-96cb-4af060db3521")]
internal sealed class FloatListToBuffer : Instance<FloatListToBuffer>
{
    [Output(Guid = "1a9b5e15-e9a7-4ed4-aa1a-2072398921b4")]
    public readonly Slot<Buffer> Buffer = new();

    public FloatListToBuffer()
    {
        Buffer.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        try
        {
            var floatParams = Params.GetValue(context);
            if (floatParams == null || floatParams.Count == 0)
                return;
            
            //var array = floatParams.ToArray();
            var arraySize = (floatParams.Count / 4 + (floatParams.Count % 4 == 0 ? 0 : 1)) * 4; // always 16byte slices for alignment
            var array = new float[arraySize];
            
            for (var i = 0; i < floatParams.Count; i++)
            {
                array[i] = floatParams[i];
            }
            
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
        catch (Exception e)
        {
            Log.Warning("Failed to creat float value buffer" + e.Message);
        }
    }
        

    [Input(Guid = "B2C0CE0C-DEAA-4067-9E18-4416BEFDC232")]
    public readonly MultiInputSlot<List<float>> Params = new();

}