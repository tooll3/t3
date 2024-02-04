using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples
{
	[Guid("5fb11919-0eef-4cee-bddf-bf30e0a98ad9")]
    public class CamPositionExample : Instance<CamPositionExample>
    {
        [Output(Guid = "d18c45be-bb7e-4727-8de2-2c227529ed76")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

