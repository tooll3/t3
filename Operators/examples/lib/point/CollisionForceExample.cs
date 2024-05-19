using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_54bea221_f2db_4ff8_afeb_200bcfd37871
{
    public class CollisionForceExample : Instance<CollisionForceExample>
    {
        [Output(Guid = "f8998bf0-e142-4a4a-80c1-a5e0b8df4178")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

