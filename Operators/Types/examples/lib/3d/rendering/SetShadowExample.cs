using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1f73d45d_fdbf_4429_8e17_741285a050f5
{
    public class SetShadowExample : Instance<SetShadowExample>
    {
        [Output(Guid = "302055d3-17af-4393-b85f-d792866a1f79")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

