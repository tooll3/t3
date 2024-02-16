using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.examples
{
	[Guid("ff01e31e-c987-449f-ab4a-066fedf5d237")]
    public class FindClosestPointsOnMeshExample : Instance<FindClosestPointsOnMeshExample>
    {
        [Output(Guid = "fa516e85-0fda-486b-b820-1acb77deea3e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

