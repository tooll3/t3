using System;
using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace user.cynic
{
	[Guid("6fae395d-c3a0-4693-a3dc-8959cda5a92b")]
    public class RandomParticleIndexBuffer : Instance<RandomParticleIndexBuffer>
    {
        [Output(Guid = "72f61c86-36bf-49cc-9263-0dcd9d617aa2")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new();

        public RandomParticleIndexBuffer()
        {
            Buffer.UpdateAction = UpdateBuffer;
        }


        [StructLayout(LayoutKind.Explicit, Size = 8)]
        struct ParticleIndex
        {
            [FieldOffset(0)]
            public int index;
            [FieldOffset(4)]
            public float squaredDistToCamera;
        }

        private void UpdateBuffer(EvaluationContext context)
        {
            int count = Count.GetValue(context);

            if (count <= 0)
                return;

            if (_data == null || count != _data.Length)
            {
                _data = new ParticleIndex[count];
                var random = new Random(0);
                for (int i = 0; i < count; i++)
                {
                    _data[i].index = i;
                    _data[i].squaredDistToCamera = random.Next(-10000, 10000);
                }
            }

            ResourceManager.SetupStructuredBuffer(_data, ref Buffer.Value);
        }

        private ParticleIndex[] _data;

        [Input(Guid = "26c21fa9-3788-42b5-a6ce-68f8907e98f3")]
        public readonly InputSlot<int> Count = new();
    }
}