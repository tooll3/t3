using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.wake.summer2022.elements
{
	[Guid("74ddc8f9-831e-4a82-945d-a9f5c53c040c")]
    public class WS4_Physarum : Instance<WS4_Physarum>
    {
        [Output(Guid = "6085f081-382e-4385-bb29-1c8ecdf98697")]
        public readonly Slot<Texture2D> Output = new();


    }
}

