using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_805d5196_f253_4fb6_9c5e_d69915b56328
{
    public class ValuesToTextureExample : Instance<ValuesToTextureExample>
    {
        [Output(Guid = "db89d748-5fa7-4f47-926b-51cc949aac41")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

