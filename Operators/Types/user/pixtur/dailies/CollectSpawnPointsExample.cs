using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1c99e732_7fd7_4254_8ab2_3a9cf2325982
{
    public class CollectSpawnPointsExample : Instance<CollectSpawnPointsExample>
    {
        [Output(Guid = "eb14540d-5d13-4b83-a2ed-32535a399815")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

