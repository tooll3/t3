using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_af0a4265_44aa_49d9_b674_5b7c1937c99a
{
    public class TextureMapForceExample : Instance<TextureMapForceExample>
    {
        [Output(Guid = "c3c883f9-6f5b-4057-bced-62a1f9a09bb1")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

