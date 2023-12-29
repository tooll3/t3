using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7bc42ff0_2987_4dff_ab48_0eb4ab11f149
{
    public class Sim2dStacking : Instance<Sim2dStacking>
    {

        [Output(Guid = "7bdda69c-318a-44ab-84ed-34167929f988")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "c2ccf891-a94c-4176-8b90-bf47cb240773")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new();

        [Input(Guid = "30e25647-54d8-417b-a7af-a62c2b39b37b")]
        public readonly InputSlot<float> Amount = new();

        [Input(Guid = "a64c3cfc-03c7-4b33-9011-42b7073ff46d")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();


        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
            WorldSpace,
        }
    }
}

