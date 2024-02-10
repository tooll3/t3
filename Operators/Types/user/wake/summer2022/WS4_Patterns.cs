using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_416ebd7f_e8ef_46a0_8048_1f5dd424cec2
{
    public class WS4_Patterns : Instance<WS4_Patterns>
    {
        [Output(Guid = "8a006310-bd6f-4de7-8a4a-fc2577ac636f")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

