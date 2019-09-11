using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types
{
    public class TimeConstBuffer : Instance<TimeConstBuffer>
    {
        [Output(Guid = "{6C118567-8827-4422-86CC-4D4D00762D87}")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new Slot<SharpDX.Direct3D11.Buffer>();

        private uint _bufferResId;

        public TimeConstBuffer()
        {
            Buffer.UpdateAction = Update;
            Buffer.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var bufferContent = new TimeBufferLayout((float)EvaluationContext.GlobalTime, (float)context.Time);
            SetupConstBufferForCS(bufferContent, ref Buffer.Value, 0);
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct TimeBufferLayout
        {
            public TimeBufferLayout(float globalTime, float time)
            {
                GlobalTime = globalTime;
                Time = time;
            }

            [FieldOffset(0)]
            public float GlobalTime;
            [FieldOffset(4)]
            public float Time;
        }       
        
        public static void SetupConstBufferForCS<T>(T bufferData, ref Buffer buffer, int slot) where T : struct
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager._device;

            using (var data = new DataStream(Marshal.SizeOf(typeof(T)), true, true))
            {
                data.Write(bufferData);
                data.Position = 0;

                if (buffer == null)
                {
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = Marshal.SizeOf(typeof(T)),
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    buffer = new Buffer(device, data, bufferDesc);
                }
                else
                {
                    device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer, 0);
                }
                device.ImmediateContext.ComputeShader.SetConstantBuffer(slot, buffer);
            }
        }
    }
}