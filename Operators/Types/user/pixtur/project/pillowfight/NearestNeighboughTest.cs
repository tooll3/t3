using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_8228a543_f725_41d9_8629_f6d85f9e858e
{
    public class NearestNeighboughTest : Instance<NearestNeighboughTest>
    {

        [Output(Guid = "c967f2b9-a4c6-4570-b212-3ce228ba2b22")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        [Input(Guid = "8e35d7a7-433d-478f-b202-bd757f4d5d72")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> PointsA_ = new();

        [Input(Guid = "15f43187-123d-4d40-8bcc-70c8c8b0392f")]
        public readonly InputSlot<float> CellSize = new();

        [Input(Guid = "3b64cb71-71dc-4f68-8daa-6b2d15bb6353")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new();
    }
}

