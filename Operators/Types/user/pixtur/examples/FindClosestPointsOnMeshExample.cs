using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ff01e31e_c987_449f_ab4a_066fedf5d237
{
    public class FindClosestPointsOnMeshExample : Instance<FindClosestPointsOnMeshExample>
    {
        [Output(Guid = "fa516e85-0fda-486b-b820-1acb77deea3e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

