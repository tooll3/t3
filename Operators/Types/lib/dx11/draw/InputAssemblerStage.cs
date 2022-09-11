using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_9d1266c5_23db_439f_a475_8000fdd1c318
{
    public class InputAssemblerStage : Instance<InputAssemblerStage>
    {
        [Output(Guid = "18CAE035-C050-4F98-9E5E-B3A6DB70DDA7", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>(new Command());

        public InputAssemblerStage()
        {
            Output.UpdateAction = Update;
            Output.Value.RestoreAction = Restore;
        }

        private Buffer[] _vertexBuffer = new Buffer[0];
        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var iaStage = deviceContext.InputAssembler;

            InputLayout.GetValue(context);
            VertexBuffers.GetValues(ref _vertexBuffer, context);
            IndexBuffer.GetValue(context);

            _prevTopology = iaStage.PrimitiveTopology;
            iaStage.PrimitiveTopology = PrimitiveTopology.GetValue(context);
        }

        public void Restore(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var iaStage = deviceContext.InputAssembler;

            iaStage.PrimitiveTopology = _prevTopology;
        }

        private PrimitiveTopology _prevTopology;


        [Input(Guid = "1EA95430-B853-4A60-A981-F316905995E8")]
        public readonly InputSlot<PrimitiveTopology> PrimitiveTopology = new InputSlot<PrimitiveTopology>();
        [Input(Guid = "B8E07473-60F9-4F5E-995D-7165EF8F7993")]
        public readonly InputSlot<InputLayout> InputLayout = new InputSlot<InputLayout>();
        [Input(Guid = "4A1703D4-5958-4EDE-A755-79A12FE85F3B")]
        public readonly MultiInputSlot<Buffer> VertexBuffers = new MultiInputSlot<Buffer>();
        [Input(Guid = "C8FD1C4B-E6D6-4CA1-A718-4518E3BFFC59")]
        public readonly InputSlot<Buffer> IndexBuffer = new InputSlot<Buffer>();
    }
}