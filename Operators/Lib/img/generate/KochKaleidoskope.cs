using System.Runtime.InteropServices;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.img.generate
{
	[Guid("f7366bdc-86b2-4951-8788-3826126ed8c2")]
    public class KochKaleidoskope : Instance<KochKaleidoskope>
    {
        [Output(Guid = "15711a30-7d0c-40f4-be68-e2e75c5b4dc5")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "362bcc93-74a0-40fb-a19f-bff8b7246c34")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "79c84135-5390-455b-bf60-53d4002d0606")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "912cfd4d-5bef-4fdc-abfc-2f5bf8239875")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

        [Input(Guid = "0b6c67e6-338e-4bb3-bcee-5cc9e2045824")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new();

        [Input(Guid = "a1b3ae23-cb29-4e3e-93b6-fd4f1efadbf1")]
        public readonly InputSlot<float> Angle = new();

        [Input(Guid = "c355fa5c-c1f5-4e80-b1b1-d2d233dea3fc")]
        public readonly InputSlot<int> Steps = new();

        [Input(Guid = "9618504d-bfbd-4d3d-b4d4-03a7064f0fea")]
        public readonly InputSlot<float> ShadeSteps = new();

        [Input(Guid = "05d0a5b2-a7a6-45c5-969a-f00150558a30")]
        public readonly InputSlot<float> Rotate = new();

        [Input(Guid = "d2e12b96-7be3-4b31-bcdd-c882543a6ca6")]
        public readonly InputSlot<float> ShadeFolds = new();

        [Input(Guid = "8866edf5-9f58-4da5-abd1-b4e25607bedc")]
        public readonly InputSlot<Int2> Resolution = new();
    }
}

