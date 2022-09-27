using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_896bc0ab_0552_4a89_a990_8febffb8b823
{
    public class GrainExample : Instance<GrainExample>
    {
        [Output(Guid = "70efcbd0-0433-4b19-beaa-730706852ff1")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

