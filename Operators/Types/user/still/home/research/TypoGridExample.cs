using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f8061538_b5c6_4c48_89be_5057e7d174d2
{
    public class TypoGridExample : Instance<TypoGridExample>
    {
        [Output(Guid = "c2d08f71-7903-42ad-a8f2-df1c6c35cb25")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

