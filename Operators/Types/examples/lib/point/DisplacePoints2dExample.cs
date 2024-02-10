using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9b5f602d_0449_453d_a08a_93430b313cb4
{
    public class DisplacePoints2dExample : Instance<DisplacePoints2dExample>
    {
        [Output(Guid = "3192d666-6b7c-4ef4-9086-684df2f387a0")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

