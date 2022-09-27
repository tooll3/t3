using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9b28e6b9_1d1f_42d8_8a9e_33497b1df820
{
    public class Draw : Instance<Draw>
    {
        [Output(Guid = "49B28DC3-FCD1-4067-BC83-E1CC848AE55C", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        public Draw()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            var deviceContext = device.ImmediateContext;

            var setVs = deviceContext.VertexShader.Get();
            var setPs = deviceContext.PixelShader.Get();
            if (setVs == null || setPs == null)
            {
                Log.Warning("Trying to issue draw call, but pixel and/or vertex shader are null.");
                return;
            }

            deviceContext.Draw(VertexCount.GetValue(context), VertexStartLocation.GetValue(context));
        }

        [Input(Guid = "8716B11A-EF71-437E-9930-BB747DA818A7")]
        public readonly InputSlot<int> VertexCount = new InputSlot<int>();
        [Input(Guid = "B381B3ED-F043-4001-9BBC-3E3915F38235")]
        public readonly InputSlot<int> VertexStartLocation = new InputSlot<int>();
    }
}