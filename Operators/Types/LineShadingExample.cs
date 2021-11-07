using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_39535040_8b56_4629_a9af_9b35399a2494
{
    public class LineShadingExample : Instance<LineShadingExample>
    {
        [Output(Guid = "6c2a03a0-0193-4e0c-b689-02cc6f534f1c")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

