using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_44f364ed_0dee_4586_a60b_77572cc8f2b7
{
    public class PairPointsForGridWalkLinesExample : Instance<PairPointsForGridWalkLinesExample>
    {
        [Output(Guid = "9302a1cc-8b0b-4cfa-9dca-1dfb8fd82600")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

