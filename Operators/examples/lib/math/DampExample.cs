using System.Numerics;
using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.math
{
	[Guid("366b6a6f-9995-48a1-bc0e-5c516ec5170e")]
    public class DampExample : Instance<DampExample>
    {
        [Output(Guid = "00f191f6-1377-42e3-8494-b9b5235c1a37")]
        public readonly Slot<Vector3> Result = new();


    }
}

