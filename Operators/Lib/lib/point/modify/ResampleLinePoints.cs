using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_13ff9adb_2634_4129_8bb4_4fb764d38be6
{
    public class ResampleLinePoints : Instance<ResampleLinePoints>
    {

        [Output(Guid = "28cba376-7037-4d8c-bc4b-a8c747687f03")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "78f5d842-960f-4885-a65b-defd04871091")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "e731ef71-b172-4308-b7d2-a59fa55b266a")]
        public readonly InputSlot<int> Count = new();

        [Input(Guid = "3d50d3c5-07e6-4246-8740-fcdc62173e1d")]
        public readonly InputSlot<float> SmoothDistance = new();

        [Input(Guid = "58980e30-204b-40e2-9610-8482ff01a57c", MappedType = typeof(SampleModes))]
        public readonly InputSlot<int> SampleMode = new();

        [Input(Guid = "354e468d-d38a-49ba-b2f3-8e522723d43f")]
        public readonly InputSlot<System.Numerics.Vector2> SampleRange = new();


        private enum SampleModes
        {
            StartEnd,
            StartLength,
        }
    }
}

