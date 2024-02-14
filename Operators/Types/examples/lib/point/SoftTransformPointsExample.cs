using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0b019a98_0470_4d98_9d34_e06abd8c72d1
{
    public class SoftTransformPointsExample : Instance<SoftTransformPointsExample>
    {
        [Output(Guid = "d4ccbf12-6e9e-461e-867b-bc72b89afc80")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

