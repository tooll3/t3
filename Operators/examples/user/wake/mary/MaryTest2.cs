using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ba2a8670_e0c3_4e7b_9382_d1c0938ba2b3
{
    public class MaryTest2 : Instance<MaryTest2>
    {
        [Output(Guid = "690dbf18-90c8-499a-b1be-28093deefe4a")]
        public readonly Slot<Texture2D> Output = new();


    }
}

