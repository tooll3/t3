using T3.Core;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_e4e608e7_1248_4d73_910c_e8bfbb34251b
{
    public class SimpleLiquid : Instance<SimpleLiquid>
    {
        [Output(Guid = "9397f264-fde0-4806-b287-5cac9fd638b1")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "5c42604f-bbc1-4e65-8f21-8d8e79179e5a")]
        public readonly InputSlot<float> ShouldReset = new InputSlot<float>();

        [Input(Guid = "a9d8c898-545e-4c0e-af61-6d2251209460")]
        public readonly InputSlot<System.Numerics.Vector2> Gravity = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "3520474e-1341-4b49-a4ce-247b52ae3fe7")]
        public readonly InputSlot<float> BorderStrength = new InputSlot<float>();

        [Input(Guid = "b4db432f-3284-4b36-9c81-b644d958f582")]
        public readonly InputSlot<float> Damping = new InputSlot<float>();

        [Input(Guid = "04dce025-be57-4478-a66c-4e92f4f1f8c4")]
        public readonly InputSlot<float> MassAttraction = new InputSlot<float>();

        [Input(Guid = "fdd814ec-b8c6-41bd-91d1-ae6e6cfb7e8c")]
        public readonly InputSlot<float> Brightness = new InputSlot<float>();

        [Input(Guid = "bee3be17-6ac1-4c11-902a-0321cf5b1a19")]
        public readonly InputSlot<float> StabilizeMass = new InputSlot<float>();

        [Input(Guid = "ac239fb9-b90f-43e9-b591-b4e4e07bb5f7")]
        public readonly InputSlot<float> StabilizeMassTarget = new InputSlot<float>();

    }
}

