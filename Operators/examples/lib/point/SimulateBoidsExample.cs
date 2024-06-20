using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("abdf2c06-2b93-4938-afac-6d737a1c1be5")]
    public class SimulateBoidsExample : Instance<SimulateBoidsExample>
    {
        [Output(Guid = "c91a8b4e-ddca-4e23-99ad-4130feeabae0")]
        public readonly Slot<Texture2D> Output = new();


    }
}

