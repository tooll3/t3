using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_02625eeb_7f7d_4c48_af5b_13d9a2079b3b
{
    public class Synchotron : Instance<Synchotron>
    {
        [Output(Guid = "ad075d63-d346-4fb9-bde6-90824d9a577e")]
        public readonly Slot<Texture2D> Output = new();


    }
}

