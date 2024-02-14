using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c1348a39_276f_4fe6_9210_f9f605cb0ece
{
    public class DrawBillboardsExample2 : Instance<DrawBillboardsExample2>
    {
        [Output(Guid = "50b6aca4-2bfe-4008-a1cf-aa7065576fb2")]
        public readonly Slot<Texture2D> ColorBuffer = new();

        [Input(Guid = "384dfff2-bcb0-4e8d-aa83-ba1fe4ddc04e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageInput = new();

        [Input(Guid = "9532a22f-4992-415b-a6da-d36e55f75690")]
        public readonly InputSlot<float> Hairiness = new();


    }
}

