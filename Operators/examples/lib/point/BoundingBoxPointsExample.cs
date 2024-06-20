using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
    [Guid("7bd3aab4-009f-4c5e-95c9-88f3d08b6893")]
    public class BoundingBoxPointsExample : Instance<BoundingBoxPointsExample>
    {
        [Output(Guid = "3edf7f96-e0ac-4370-a099-fcfe30fee2dc")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

