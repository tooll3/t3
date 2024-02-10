using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2ff86df2_6492_4996_bd5e_fc12ec2e0947
{
    public class AsciiRenderExample : Instance<AsciiRenderExample>
    {
        [Output(Guid = "20cdf670-2e51-44ab-9af2-3ff2c58485fd")]
        public readonly Slot<Texture2D> TextureOut = new();


    }
}

