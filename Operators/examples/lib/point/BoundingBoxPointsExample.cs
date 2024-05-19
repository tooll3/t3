using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7bd3aab4_009f_4c5e_95c9_88f3d08b6893
{
    public class BoundingBoxPointsExample : Instance<BoundingBoxPointsExample>
    {
        [Output(Guid = "3edf7f96-e0ac-4370-a099-fcfe30fee2dc")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

