using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ea7536a2_f69b_4964_a04c_4474bfacfa56
{
    public class PixelateExample : Instance<PixelateExample>
    {
        [Output(Guid = "32ae7d3f-5fcd-474a-8f5e-99ffe28f7b60")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

