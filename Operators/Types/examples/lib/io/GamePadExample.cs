using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_025285fd_c00f_44c7_8698_363ccc763fa1
{
    public class GamePadExample : Instance<GamePadExample>
    {
        [Output(Guid = "df202c9a-aad1-4ca8-a12d-a61e48bdb415")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

