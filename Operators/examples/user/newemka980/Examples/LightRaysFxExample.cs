using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_82159382_64e7_4aef_bc23_6a0f0f40f18d
{
    public class LightRaysFxExample : Instance<LightRaysFxExample>
    {
        [Output(Guid = "a91be719-68e5-439a-93f6-3c66aa00e14e")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

