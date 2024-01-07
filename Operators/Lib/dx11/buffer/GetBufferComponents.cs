using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.dx11.buffer
{
	[Guid("80dff680-5abf-484a-b9e0-81d72f3b7aa4")]
    public class GetBufferComponents : Instance<GetBufferComponents>
    {
        [Output(Guid = "a7d11905-eb9e-42a4-a077-11d2c1cb41b2")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new();

        [Output(Guid = "1368ab8e-d75e-429f-8ecd-0944f3ede9ab")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new();

        [Output(Guid = "f03246a7-e39f-4a41-a0c3-22bc976a6000")]
        public readonly Slot<UnorderedAccessView> UnorderedAccessView = new();

        [Output(Guid = "D7918FD8-906E-424D-8C5C-9631941CFC9D")]
        public readonly Slot<int> Length = new();

        public GetBufferComponents()
        {
            Buffer.UpdateAction = Update;
            ShaderResourceView.UpdateAction = Update;
            UnorderedAccessView.UpdateAction = Update;
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var bufferWithViews = BufferWithViews.GetValue(context);
            if (bufferWithViews != null)
            {
                Buffer.Value = bufferWithViews.Buffer;
                ShaderResourceView.Value = bufferWithViews.Srv;
                UnorderedAccessView.Value = bufferWithViews.Uav;
                if (ShaderResourceView?.Value == null)
                {
                    Log.Warning("Can't access undefined shader resource view", this);
                    SetAsInvalid();
                }
                else
                {
                    try
                    {
                        Length.Value = ShaderResourceView.Value.Description.Buffer.ElementCount;
                    }
                    catch (Exception e)
                    {
                        Log.Warning("Can't access undefined SRV description: " + e.Message, this);
                        SetAsInvalid();
                    }
                    
                }
            }
            else
            {
                SetAsInvalid();
            }
            
        }

        private void SetAsInvalid()
        {
            Buffer.Value = null;
            ShaderResourceView.Value = null;
            UnorderedAccessView.Value = null;
            Length.Value = 0;
        }

        [Input(Guid = "7a13b834-21e5-4cef-ad5b-23c3770ea763")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BufferWithViews = new();
    }
}