using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c9a7e71c_7311_4d1e_8508_cd580d7a0b8d
{
    public class Numbers : Instance<Numbers>
    {
        [Output(Guid = "80E2C6C3-1282-4075-B4E3-86BD67795F1C")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        [Output(Guid = "{3A9734E8-7346-4535-BD54-3B5A735CC6B8}")]
        public readonly Slot<string> Output = new Slot<string>("Project Output");
    }
}