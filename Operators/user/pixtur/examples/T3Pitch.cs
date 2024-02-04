using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.examples
{
	[Guid("cfdf9331-07e9-46ce-b62c-efec76fc9c9e")]
    public class T3Pitch : Instance<T3Pitch>
    {
        [Output(Guid = "827e06e0-4c7c-40c5-90bb-72cc8ad3fedb")]
        public readonly Slot<Texture2D> Output = new();


    }
}

