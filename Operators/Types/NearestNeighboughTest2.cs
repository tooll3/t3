using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f61ceb9b_74f8_4883_88ea_7e6c35b63bbd
{
    public class NearestNeighboughTest2 : Instance<NearestNeighboughTest2>
    {

        [Output(Guid = "9a01a420-8611-410e-8b6a-61a3c8988a5e")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "4bae9eaa-42d8-4c1c-8776-3abebcce20e2")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> PointsA_ = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "22f9737b-b3b4-4455-a4ec-8d61ab7abc6c")]
        public readonly InputSlot<float> CellSize = new InputSlot<float>();

        [Input(Guid = "5f11283d-be8a-4b91-8af6-6575b2249d82")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Input(Guid = "959f6c5a-c19b-492e-9e4f-1e758e261ff5")]
        public readonly InputSlot<int> CenterPointIndex = new InputSlot<int>();

        [Input(Guid = "bfc32514-26e7-4cb4-bc45-21b5c1a02307")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();
    }
}

