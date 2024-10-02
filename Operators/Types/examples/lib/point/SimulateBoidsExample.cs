using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_abdf2c06_2b93_4938_afac_6d737a1c1be5
{
    public class SimulateBoidsExample : Instance<SimulateBoidsExample>
    {
        [Output(Guid = "c91a8b4e-ddca-4e23-99ad-4130feeabae0")]
        public readonly Slot<Texture2D> Output = new();


    }
}

