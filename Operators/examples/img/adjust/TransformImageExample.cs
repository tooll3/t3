using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.img.adjust
{
	[Guid("af89cc41-67ab-4ef8-8a63-ce0de82d8652")]
    public class TransformImageExample : Instance<TransformImageExample>
    {
        [Output(Guid = "d7ba385e-4168-4c1e-bde2-18343e0a9d1e")]
        public readonly Slot<Texture2D> Output = new();


    }
}

