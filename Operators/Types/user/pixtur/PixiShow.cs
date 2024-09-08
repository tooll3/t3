using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_75bd09ea_ce64_4cc5_b718_e42d6b4e4079
{
    public class PixiShow : Instance<PixiShow>
    {
        [Output(Guid = "953d96d1-19ae-4698-8386-192695dd2951")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

