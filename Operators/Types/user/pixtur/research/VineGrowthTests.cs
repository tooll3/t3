using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_49f58668_8a36_48f3_b162_c33c98dc0675
{
    public class VineGrowthTests : Instance<VineGrowthTests>
    {
        [Output(Guid = "fa9545a5-4f06-4841-b83a-8bdcf1407dfc")]
        public readonly Slot<Texture2D> Output = new();


    }
}

