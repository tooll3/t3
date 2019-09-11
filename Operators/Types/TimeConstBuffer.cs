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
            var bufferContent = new TimeBufferLayout((float)EvaluationContext.GlobalTime, (float)context.Time, Param1.GetValue(context),
                                                     Param2.GetValue(context), Param3.GetValue(context), Param4.GetValue(context), Param5.GetValue(context),
                                                     Param6.GetValue(context));
            SetupConstBufferForCS(bufferContent, ref Buffer.Value, 0);
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public struct TimeBufferLayout
        {
            public TimeBufferLayout(float globalTime, float time, float param1, float param2, float param3, float param4, float param5, float param6)
            {
                GlobalTime = globalTime;
                Time = time;
                Param1 = param1;
                Param2 = param2;
                Param3 = param3;
                Param4 = param4;
                Param5 = param5;
                Param6 = param6;
            }

            [FieldOffset(0)]
            public float GlobalTime;
            [FieldOffset(4)]
            public float Time;
            [FieldOffset(8)]
            public float Param1;
            [FieldOffset(12)]
            public float Param2;
            [FieldOffset(16)]
            public float Param3;
            [FieldOffset(20)]
            public float Param4;
            [FieldOffset(24)]
            public float Param5;
            [FieldOffset(28)]
            public float Param6;
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

        [Input(Guid = "{3BB4BAB9-96CD-4E08-BAB7-2A15FB4F11C0}")]
        public readonly InputSlot<float> Param1 = new InputSlot<float>();
        [Input(Guid = "{A3E13E00-835D-4857-B804-A1E300EBCA2A}")]
        public readonly InputSlot<float> Param2 = new InputSlot<float>();
        [Input(Guid = "{E3F6E24D-97D7-4679-8A4C-C7ADA2E01022}")]
        public readonly InputSlot<float> Param3 = new InputSlot<float>();
        [Input(Guid = "{B2208D3A-F321-4F4B-81DE-6EB3026FE39C}")]
        public readonly InputSlot<float> Param4 = new InputSlot<float>();
        [Input(Guid = "{5A2AD659-66D8-4141-8198-97BF6D4DBDE5}")]
        public readonly InputSlot<float> Param5 = new InputSlot<float>();
        [Input(Guid = "{D7438941-ADD5-4288-B711-69F639110E57}")]
        public readonly InputSlot<float> Param6 = new InputSlot<float>();
    }
}