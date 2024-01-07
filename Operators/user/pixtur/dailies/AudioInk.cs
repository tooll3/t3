using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.dailies
{
	[Guid("89682afb-0c4a-4142-9cb0-6e84e322a4ea")]
    public class AudioInk : Instance<AudioInk>
    {
        [Output(Guid = "2e4d4017-dbc4-43f2-9309-c947bd6cdb32")]
        public readonly Slot<Texture2D> Output = new();


    }
}

