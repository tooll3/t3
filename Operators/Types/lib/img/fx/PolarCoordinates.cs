using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_941410e5_2023_43c6_87b1_55386bb048ac
{
    public class PolarCoordinates : Instance<PolarCoordinates>
    {
        [Output(Guid = "1758e789-809c-430a-a5c8-22fd8bbe5e54")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


        [Input(Guid = "dfa1d71d-2964-41c2-bb04-86a39c36ce6e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "4c07b8bd-c78d-44ce-bb95-8114284af2bf")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "83f2dc32-5830-4558-9137-f6805f3f6ff6")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "bfcb7f48-517a-4791-96fa-4aa26862839b")]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "d796cbc2-26b4-4805-b1b7-bd23b42d1984")]
        public readonly InputSlot<float> RadialBias = new InputSlot<float>();

        [Input(Guid = "6f17f1b4-0b5e-4934-8be1-c8198443fac3")]
        public readonly InputSlot<float> RadialOffset = new InputSlot<float>();

        [Input(Guid = "627847c4-5ba3-4852-bb09-2b0d99e05451")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "4bd38d2f-0c52-456f-9938-b1f571c719fe")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "caa14f08-f004-4af8-a448-116e21106d5b")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();
    }
}

