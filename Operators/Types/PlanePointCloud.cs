using System;
using System.Runtime.InteropServices;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_bd175754_d3fd_4c75_9d40_5023eb1d8db6
{
    public class PlanePointCloud : Instance<PlanePointCloud>
    {
        [Output(Guid = "eab03e8d-ce3e-42b4-9584-99365ba60889")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> PointCloudSrv = new Slot<SharpDX.Direct3D11.ShaderResourceView>();

        [Output(Guid = "44383d67-2695-4dfb-8d14-4e04b1de4d63")]
        public readonly Slot<int> EmitterId = new Slot<int>();

        public PlanePointCloud()
        {
            _id = EmitterCounter.GetId();
            EmitterId.Value = _id;
            PointCloudSrv.UpdateAction = Update;
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct BufferEntry
        {
            [FieldOffset(0)]
            public SharpDX.Vector3 Pos;

            [FieldOffset(12)]
            public int Id;

            [FieldOffset(16)]
            public SharpDX.Vector4 Color;
        }

        private Buffer _buffer;
        private int _id;

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            var numEntries = Count.GetValue(context);
            numEntries = Math.Max(numEntries, 1);
            numEntries = Math.Min(numEntries, 500000);

            var bufferData = new BufferEntry[numEntries];

            var color = Color.GetValue(context);
            var random = new Random(Seed.GetValue(context));
            var size = Size.GetValue(context);
            var end = size * 0.5f;
            var start = -end;
            var objectToWorld = context.ObjectToWorld;

            for (int index = 0; index < numEntries; index++)
            {
                float x = random.NextFloat(start.X, end.X);
                float y = random.NextFloat(start.Y, end.Y);
                var posInObject = new Vector3(x, y, 0.0f);
                var posInWorld = Vector3.Transform(posInObject, objectToWorld);

                bufferData[index].Pos = new Vector3(posInWorld.X, posInWorld.Y, posInWorld.Z);
                bufferData[index].Id = _id;
                bufferData[index].Color = new Vector4(color.X, color.Y, color.Z, color.W);
            }

            var stride = 32;
            resourceManager.SetupStructuredBuffer(bufferData, stride * numEntries, stride, ref _buffer);
            resourceManager.CreateStructuredBufferSrv(_buffer, ref PointCloudSrv.Value);
        }

        [Input(Guid = "2de69331-00ed-4612-b6c1-f2131390c735")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "4cdbd9dc-7e8a-4c27-b56f-b87a02dbb43c")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "74e67e10-02a3-49e7-9c62-6e2063d8a507")]
        public readonly InputSlot<System.Numerics.Vector2> Size = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "e22efe7b-88e6-4e35-a3eb-7fd80dc2478c")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();
    }
}