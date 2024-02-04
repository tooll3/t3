using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.point.combine
{
	[Guid("ab6ab34f-4c6e-41ef-af69-48bb9393651b")]
    public class PairPointsForLines : Instance<PairPointsForLines>
    {

        [Output(Guid = "943d1c32-e4e7-4502-8927-543b13ed0283")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        [Input(Guid = "b571a5e9-abec-47df-ac07-a7d1e60163d9")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "e14366ed-0591-424d-a8ab-36f8380dd614")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GTargets = new();

        [Input(Guid = "10a6c6d5-9685-4bde-a1b6-e5456c886898")]
        public readonly InputSlot<bool> SetWTo01 = new();
    }
}

