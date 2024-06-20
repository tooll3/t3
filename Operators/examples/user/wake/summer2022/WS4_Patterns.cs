using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.wake.summer2022
{
	[Guid("416ebd7f-e8ef-46a0-8048-1f5dd424cec2")]
    public class WS4_Patterns : Instance<WS4_Patterns>
    {
        [Output(Guid = "8a006310-bd6f-4de7-8a4a-fc2577ac636f")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

