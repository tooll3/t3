using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9645edf6_204a_45d6_9198_b9257fe071dc
{
    public class CarpeDiem : Instance<CarpeDiem>
    {
        [Output(Guid = "9ce5f91a-3547-4666-b9d5-b139007004fd")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

