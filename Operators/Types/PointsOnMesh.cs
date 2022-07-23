using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_17188f49_1243_4511_a46c_1804cae10768
{
    public class PointsOnMesh : Instance<PointsOnMesh>
    {

        [Output(Guid = "414724f2-1c7f-406a-a209-cfb3f6ad0265")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> ResultPoints = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "d737860b-2864-472d-ad46-4061b63a12d4")]
        public readonly InputSlot<float> Seed = new InputSlot<float>();

        [Input(Guid = "b3d526ee-b1f1-4254-b702-1980d659e557")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "3289dbe7-14f0-4bf0-b223-bb57419cf179")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "ed758e72-6977-44e0-a997-bf0ad83f6ceb")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();
    }
}

