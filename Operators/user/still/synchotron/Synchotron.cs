using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.synchotron
{
	[Guid("02625eeb-7f7d-4c48-af5b-13d9a2079b3b")]
    public class Synchotron : Instance<Synchotron>
    {
        [Output(Guid = "ad075d63-d346-4fb9-bde6-90824d9a577e")]
        public readonly Slot<Texture2D> Output = new();


    }
}

