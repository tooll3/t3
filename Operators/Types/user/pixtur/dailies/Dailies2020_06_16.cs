using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_58beef77_d3ed_4239_8e48_44aa2546dd39
{
    public class Dailies2020_06_16 : Instance<Dailies2020_06_16>
    {
        [Output(Guid = "5401bfcd-747f-41f6-bc10-e650198b18bf")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "1c531799-0580-4814-818c-21ce444532ae")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();


    }
}

