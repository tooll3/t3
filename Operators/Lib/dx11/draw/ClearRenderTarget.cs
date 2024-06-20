using System.Runtime.InteropServices;
using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace lib.dx11.draw
{
	[Guid("e3596381-c118-4e2e-a482-83049a9f74af")]
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
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            // deviceContext.Draw2(VertexCount.GetValue(context), VertexStartLocation.GetValue(context));
            var rtv = RenderTarget.GetValue(context);
            if (rtv == null)
                return;

            var c = ClearColor.GetValue(context);
            deviceContext.ClearRenderTargetView(rtv, new RawColor4(c.X, c.Y, c.Z, c.W));
        }

        [Input(Guid = "D73D2FE7-1AF4-48D6-9AD3-F8C87C3369D6")]
        public readonly InputSlot<Vector4> ClearColor = new();
        [Input(Guid = "25C0C15C-5B95-4FE3-8D59-4E127FCE1CF2")]
        public readonly InputSlot<RenderTargetView> RenderTarget = new();
    }
}