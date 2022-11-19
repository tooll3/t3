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

        [Input(Guid = "5e6de9ce-c291-40fc-bbe4-cd74ebbe1434")]
        public readonly InputSlot<bool> TriggerReset = new InputSlot<bool>();

        [Input(Guid = "0f3dd599-095e-4766-9483-c8cb21f9571a")]
        public readonly InputSlot<System.Numerics.Vector2> Gravity = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "faf738e5-5ff4-4bcd-85dc-b1c1484edfa6")]
        public readonly InputSlot<float> BorderStrength = new InputSlot<float>();

        [Input(Guid = "225df965-a92b-45d8-abae-81238aada793")]
        public readonly InputSlot<float> MassAttraction = new InputSlot<float>();

        [Input(Guid = "76efee74-fe2d-4926-98d8-82be384f298e")]
        public readonly InputSlot<System.Numerics.Vector4> ApplyFxTexture = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c07bdf4d-8395-4327-b57e-6ba38f36e8a0")]
        public readonly InputSlot<float> SpeedFactor = new InputSlot<float>();

        [Input(Guid = "18d0f554-e8f7-4b0f-98c7-24c9742b66af")]
        public readonly InputSlot<float> StabilizeFactor = new InputSlot<float>();

        [Input(Guid = "b5e1cded-fded-4769-8d1e-38598cf944db")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> FxTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "4e00796d-704a-4c1f-9822-d11e1ada619c")]
        public readonly InputSlot<SharpDX.DXGI.Format> Format = new InputSlot<SharpDX.DXGI.Format>();

        [Input(Guid = "b4d844b5-5d29-4c2d-a327-e1b9eb7411d8")]
        public readonly InputSlot<int> Iterations = new InputSlot<int>();

    }
}

