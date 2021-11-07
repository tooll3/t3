using System.Security.AccessControl;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_fbd7f0f0_36a3_4fbb_91e1_cb33d4666d09
{
    public class Rasterizer : Instance<Rasterizer>
    {
        [Output(Guid = "C723AD69-FF0C-47B2-9327-BD27C0D7B6D1", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>(new Command());

        public Rasterizer()
        {
            Output.UpdateAction = Update;
            Output.Value.RestoreAction = Restore;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var rasterizer = deviceContext.Rasterizer;

            ScissorRectangles.GetValue(context);
            _prevViewports = rasterizer.GetViewports<RawViewportF>();
            Viewports.GetValues(ref _viewports, context);
            rasterizer.State = RasterizerState.GetValue(context);

            if (_viewports.Length > 0)
                rasterizer.SetViewports(_viewports, _viewports.Length);
        }

        private void Restore(EvaluationContext context)
        {
            var deviceContext = ResourceManager.Instance().Device.ImmediateContext;
            var rasterizer = deviceContext.Rasterizer;
            rasterizer.SetViewports(_prevViewports, _prevViewports.Length);
        }

        private RawViewportF[] _viewports = new RawViewportF[0];
        private RawViewportF[] _prevViewports;

        [Input(Guid = "35A52074-1E82-4352-91C3-D8E464F73BC7")]
        public readonly InputSlot<SharpDX.Direct3D11.RasterizerState> RasterizerState = new InputSlot<SharpDX.Direct3D11.RasterizerState>();
        [Input(Guid = "73945E5D-3C3C-4742-B341-A061B0DC116F")]
        public readonly MultiInputSlot<RawViewportF> Viewports = new MultiInputSlot<RawViewportF>();
        [Input(Guid = "3F71BE22-9DC2-4E47-8B3A-1EF3C9ECBD9D")]
        public readonly MultiInputSlot<RawRectangle> ScissorRectangles = new MultiInputSlot<RawRectangle>();
    }
}