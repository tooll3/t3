using T3.Core;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_d1a1f207_0537_416a_985b_e350c3f7e655
{
    public class SimpleLiquid2 : Instance<SimpleLiquid2>
    {
        [Output(Guid = "08692782-19d4-49fe-94e7-1209500ed1d8")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "fddaca10-39db-4cc0-8b0a-06e9319b0180")]
        public readonly InputSlot<float> ShouldReset = new InputSlot<float>();

        [Input(Guid = "0f3dd599-095e-4766-9483-c8cb21f9571a")]
        public readonly InputSlot<System.Numerics.Vector2> Gravity = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "faf738e5-5ff4-4bcd-85dc-b1c1484edfa6")]
        public readonly InputSlot<float> BorderStrength = new InputSlot<float>();

        [Input(Guid = "0dab731e-e419-4244-a694-2dfb2e372750")]
        public readonly InputSlot<float> Damping = new InputSlot<float>();

        [Input(Guid = "225df965-a92b-45d8-abae-81238aada793")]
        public readonly InputSlot<float> MassAttraction = new InputSlot<float>();

        [Input(Guid = "013b2675-0a53-4b5d-be3c-3b52cbb37134")]
        public readonly InputSlot<float> Brightness = new InputSlot<float>();

        [Input(Guid = "6097721e-d139-444d-bc3f-c539fe6ceadd")]
        public readonly InputSlot<float> StabilizeMass = new InputSlot<float>();

        [Input(Guid = "aaaa20e2-7bad-4bb0-a656-115876e021bc")]
        public readonly InputSlot<float> StabilizeMassTarget = new InputSlot<float>();

        [Input(Guid = "b5e1cded-fded-4769-8d1e-38598cf944db")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> FxTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "76efee74-fe2d-4926-98d8-82be384f298e")]
        public readonly InputSlot<System.Numerics.Vector4> ApplyFxTexture = new InputSlot<System.Numerics.Vector4>();

    }
}

