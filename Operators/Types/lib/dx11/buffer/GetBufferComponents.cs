using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_80dff680_5abf_484a_b9e0_81d72f3b7aa4
{
    public class GetBufferComponents : Instance<GetBufferComponents>
    {
        [Output(Guid = "a7d11905-eb9e-42a4-a077-11d2c1cb41b2")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new Slot<SharpDX.Direct3D11.Buffer>();

        [Output(Guid = "1368ab8e-d75e-429f-8ecd-0944f3ede9ab")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new Slot<ShaderResourceView>();

        [Output(Guid = "f03246a7-e39f-4a41-a0c3-22bc976a6000")]
        public readonly Slot<UnorderedAccessView> UnorderedAccessView = new Slot<UnorderedAccessView>();

        public GetBufferComponents()
        {
            Buffer.UpdateAction = Update;
            ShaderResourceView.UpdateAction = Update;
            UnorderedAccessView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var bufferWithViews = BufferWithViews.GetValue(context);
            if (bufferWithViews != null)
            {
                Buffer.Value = bufferWithViews.Buffer;
                ShaderResourceView.Value = bufferWithViews.Srv;
                UnorderedAccessView.Value = bufferWithViews.Uav;
            }
            else
            {
                Buffer.Value = null;
                ShaderResourceView.Value = null;
                UnorderedAccessView.Value = null;
            }
        }

        [Input(Guid = "7a13b834-21e5-4cef-ad5b-23c3770ea763")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BufferWithViews = new InputSlot<T3.Core.DataTypes.BufferWithViews>();
    }
}