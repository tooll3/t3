using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9a9e4a84_c9c6_446c_a76c_fb70927767d8
{
    public class CoC : Instance<CoC>
    {
        [Output(Guid = "2b627b61-3261-4873-9c1b-b2c0fa50c158")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "b97aae60-5de4-4a41-9f83-ef56a43cba8e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "5d108062-cd1f-4541-9ec8-04aaa00d9f73")]
        public readonly InputSlot<float> Near = new InputSlot<float>();

        [Input(Guid = "607f86a7-7d39-4027-b563-06b599ec8efc")]
        public readonly InputSlot<float> Far = new InputSlot<float>();

        [Input(Guid = "3d3b01e0-7324-48d4-8fd1-10af04f618ff")]
        public readonly InputSlot<System.Numerics.Vector2> CoCNear = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "28a19bb7-84f0-4860-b668-c8477cff6653")]
        public readonly InputSlot<System.Numerics.Vector2> CoCFar = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b5a464f2-5f28-46c4-920b-b31f5ab2aad9")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> OutputTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

    }
}

