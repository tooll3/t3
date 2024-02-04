using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.pinky
{
	[Guid("ace28a0c-b71a-41eb-bd50-b57da38a23ce")]
    public class PinkyDemo : Instance<PinkyDemo>
    {

        [Output(Guid = "6cb5826d-6999-4d5c-bf10-badc690666e8")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();


    }
}

