using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3dd015ed_5d7a_4b0e_a6da_958c58f716bb
{
    public class GridPositionExample : Instance<GridPositionExample>
    {
        [Output(Guid = "a2de29e8-2a27-47c9-b99e-5b1ce4dc10b5")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

