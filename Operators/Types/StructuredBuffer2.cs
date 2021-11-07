using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Resource = T3.Core.Resource;

namespace T3.Operators.Types.Id_b6c5be1d_b133_45e9_a269_8047ea0d6ad7
{
    public class StructuredBuffer2 : Instance<StructuredBuffer2>
    {

        [Output(Guid = "c997268d-6709-49de-980e-64d7a47504f7")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> BufferWithViews = new Slot<T3.Core.DataTypes.BufferWithViews>();

        public StructuredBuffer2()
        {
            BufferWithViews.UpdateAction = UpdateBuffer;
        }

        private void UpdateBuffer(EvaluationContext context)
        {
            int stride = Stride.GetValue(context);
            int sizeInBytes = stride * Count.GetValue(context);
            bool createSrv = CreateSrv.GetValue(context);
            bool createUav = CreateUav.GetValue(context);
            var bufferFlags = BufferFlags.GetValue(context);

            if (sizeInBytes <= 0)
                return;

            if (BufferWithViews.Value == null)
                BufferWithViews.Value = new BufferWithViews();

            ResourceManager.Instance().SetupStructuredBuffer(sizeInBytes, stride, ref BufferWithViews.Value.Buffer);

            if (createSrv)
                ResourceManager.Instance().CreateStructuredBufferSrv(BufferWithViews.Value.Buffer, ref BufferWithViews.Value.Srv);

            if (createUav)
                ResourceManager.Instance().CreateStructuredBufferUav(BufferWithViews.Value.Buffer, bufferFlags, ref BufferWithViews.Value.Uav);
        }

        [Input(Guid = "0016dd87-8756-4a97-a0da-096e1a879c05")]
        public readonly InputSlot<int> Stride = new InputSlot<int>();

        [Input(Guid = "16f98211-fe97-4235-b33a-ddbbd2b5997f")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "bb5fa9b9-1155-47f5-9ed5-7832826f3df2")]
        public readonly InputSlot<bool> CreateSrv = new InputSlot<bool>();

        [Input(Guid = "dd0db46d-a6a0-4d84-9dd4-ab805e2197fb")]
        public readonly InputSlot<bool> CreateUav = new InputSlot<bool>();
        
        [Input(Guid = "43c2b314-4809-4022-9b07-99965e5c1a7a")]
        public readonly InputSlot<UnorderedAccessViewBufferFlags> BufferFlags = new InputSlot<UnorderedAccessViewBufferFlags>();
    }
}