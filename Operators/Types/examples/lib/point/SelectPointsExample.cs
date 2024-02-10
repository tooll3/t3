using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2fcb6afc_2d82_47a5_a6dd_39c85348b8c4
{
    public class SelectPointsExample : Instance<SelectPointsExample>
    {
        [Output(Guid = "015dd888-70a7-412c-a2da-daada267c1ae")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

