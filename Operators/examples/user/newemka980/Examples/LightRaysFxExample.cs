using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.newemka980.Examples
{
    [Guid("82159382-64e7-4aef-bc23-6a0f0f40f18d")]
    public class LightRaysFxExample : Instance<LightRaysFxExample>
    {
        [Output(Guid = "a91be719-68e5-439a-93f6-3c66aa00e14e")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

