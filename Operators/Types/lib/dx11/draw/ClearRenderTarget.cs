using System.Numerics;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_e3596381_c118_4e2e_a482_83049a9f74af
{
    public class ClearRenderTarget : Instance<ClearRenderTarget>
    {
        [Output(Guid = "A6C06F65-1738-4DD0-9D0F-728864FF521B", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        public ClearRenderTarget()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var c = ClearColor.GetValue(context);
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;

            var rtv = RenderTarget.GetValue(context);
            if (rtv != null)
            {
                deviceContext.ClearRenderTargetView(rtv, new RawColor4(c.X, c.Y, c.Z, c.W));
            }
            
            var dsv = DepthStencilView.GetValue(context);
            if (dsv != null)
            {
                deviceContext.ClearDepthStencilView(dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            }
        }

        [Input(Guid = "D73D2FE7-1AF4-48D6-9AD3-F8C87C3369D6")]
        public readonly InputSlot<System.Numerics.Vector4> ClearColor = new();
        
        [Input(Guid = "25C0C15C-5B95-4FE3-8D59-4E127FCE1CF2")]
        public readonly InputSlot<SharpDX.Direct3D11.RenderTargetView> RenderTarget = new();
        
        [Input(Guid = "65077B57-F9EB-48AA-8195-588F906B0E72")]
        public readonly InputSlot<SharpDX.Direct3D11.DepthStencilView> DepthStencilView = new();

        
    }
}