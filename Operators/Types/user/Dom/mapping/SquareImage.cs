using SharpDX.Direct3D11;
using SharpDX.Direct3D11;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9dc87ab7_85cd_4f88_9d52_f323f8df87c3
{
    public class SquareImage : Instance<SquareImage>
    {

        [Output(Guid = "bebebcd3-6894-4ff0-b7db-91aa3f9c5987")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Texture = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Output(Guid = "eec35ed1-7ef7-4b4d-8f38-55ac1d614661")]
        public readonly Slot<SharpDX.Size2> Resolution = new Slot<SharpDX.Size2>();

        [Input(Guid = "55b08c47-edc8-49f0-a2c1-02e39a6e344d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Input = new InputSlot<SharpDX.Direct3D11.Texture2D>();

    }
}

