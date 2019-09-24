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

        public TimeConstBuffer()
        {
            Buffer.UpdateAction = Update;
            Buffer.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        private void Update(EvaluationContext context)
        {
            var bufferContent = new TimeBufferLayout((float)EvaluationContext.GlobalTime, (float)context.Time);
            ResourceManager.Instance().SetupConstBufferForCS(bufferContent, ref Buffer.Value, 0);
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
        
    }
}