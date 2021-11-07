using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d392d4af_4c78_4f4a_bc3f_4c54c8c73538
{
    public class Glow : Instance<Glow>
    {
        [Output(Guid = "2ce1453b-432b-4d12-8fb7-d883e3d0c136")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> ImgOutput = new Slot<SharpDX.Direct3D11.Texture2D>();
        
        [Output(Guid = "78523193-3df8-4189-88c0-46091d53892e")]
        public readonly Slot<Command> Output = new Slot<Command>();



        [Input(Guid = "f6bdd487-c16e-4fb0-bfba-b3801f121314")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();

        [Input(Guid = "57968725-0a45-44f9-a9a2-f74c10b728e8")]
        public readonly InputSlot<float> BlurRadius = new InputSlot<float>();

        [Input(Guid = "353ac2ee-aed3-4614-adf5-e1328768fd0b")]
        public readonly InputSlot<float> Samples = new InputSlot<float>();

        [Input(Guid = "4927a3fc-87ff-44e7-88c0-499e3efcca55")]
        public readonly InputSlot<float> GlowAmount = new InputSlot<float>();

        [Input(Guid = "4c9b9135-f27b-414e-bed7-f9e5640dc526")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();

    }
}

