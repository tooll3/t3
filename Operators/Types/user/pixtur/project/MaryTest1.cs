using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2f61dfb3_2ee0_42cb_93e2_419b505c11ee
{
    public class MaryTest1 : Instance<MaryTest1>
    {
        [Output(Guid = "4bfbb025-e128-4471-b730-6507e843ae0a")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

