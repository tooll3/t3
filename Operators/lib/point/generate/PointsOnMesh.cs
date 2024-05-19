using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_17188f49_1243_4511_a46c_1804cae10768
{
    public class PointsOnMesh : Instance<PointsOnMesh>
    {

        [Output(Guid = "414724f2-1c7f-406a-a209-cfb3f6ad0265")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> ResultPoints = new();

        [Output(Guid = "dedeebfb-07a9-4ab3-98fc-18d5dd6b1d33")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Colors = new();

        [Input(Guid = "d737860b-2864-472d-ad46-4061b63a12d4")]
        public readonly InputSlot<float> Seed = new();

        [Input(Guid = "b3d526ee-b1f1-4254-b702-1980d659e557")]
        public readonly InputSlot<int> Count = new();

        [Input(Guid = "3289dbe7-14f0-4bf0-b223-bb57419cf179")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "ed758e72-6977-44e0-a997-bf0ad83f6ceb")]
        public readonly InputSlot<bool> IsEnabled = new();

        [Input(Guid = "132584c0-c27c-448a-b31d-ae72f0fb4baa")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new();

        [Input(Guid = "1843683c-53a2-4862-a9a5-4b3afe729ace")]
        public readonly InputSlot<bool> UseVertexSelection = new();
    }
}

