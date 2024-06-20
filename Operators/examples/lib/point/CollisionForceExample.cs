using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("54bea221-f2db-4ff8-afeb-200bcfd37871")]
    public class CollisionForceExample : Instance<CollisionForceExample>
    {
        [Output(Guid = "f8998bf0-e142-4a4a-80c1-a5e0b8df4178")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

