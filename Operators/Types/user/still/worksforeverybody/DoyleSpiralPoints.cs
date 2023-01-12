using T3.Core.DataTypes;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4895a804_84df_4642_a2d3_db34a268d887
{
    public class DoyleSpiralPoints : Instance<DoyleSpiralPoints>
    {
        [Output(Guid = "16e5ed75-fdef-4cea-9c20-0c68e156311b")]
        public readonly Slot<BufferWithViews> OutBuffer = new Slot<BufferWithViews>();

        [Input(Guid = "5ab2f357-5845-435e-9339-df06b57a5cf4")]
        public readonly InputSlot<float> Points = new InputSlot<float>();

        [Input(Guid = "f7f2b987-32be-4aac-a513-3331cd6abf07")]
        public readonly InputSlot<float> PointsPerRing = new InputSlot<float>();

        [Output(Guid = "ee903f87-89fc-48d9-8833-b36ec20ac726")]
        public readonly Slot<BufferWithViews> OutBuffer2 = new Slot<BufferWithViews>();


    }
}

