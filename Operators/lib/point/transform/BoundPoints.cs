using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ffbbef55_3149_48c1_95cf_cad691ce15fe
{
    public class BoundPoints : Instance<BoundPoints>
    {

        [Output(Guid = "f5a9bf63-6a41-47ee-9670-b432d50c957d")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "5090be65-7a9b-4a62-8b61-127c11495e62")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "86625813-88a5-42a7-84cb-ef5854360345")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new();

        [Input(Guid = "4d6700f1-23d3-4bfd-9563-a0b7beaa1795")]
        public readonly InputSlot<System.Numerics.Vector3> Size = new();

        [Input(Guid = "97aa1a22-55b5-42d5-896a-e4a79e0c9703")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();


        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
            WorldSpace,
        }
    }
}

