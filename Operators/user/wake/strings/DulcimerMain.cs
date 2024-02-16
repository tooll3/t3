using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.strings
{
	[Guid("4c6d2682-e92a-46c1-9f16-19d61fb1fce5")]
    public class DulcimerMain : Instance<DulcimerMain>
    {

        [Output(Guid = "9569f79b-2045-4b69-b87a-8b1cc09ef275")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output3 = new();


    }
}

