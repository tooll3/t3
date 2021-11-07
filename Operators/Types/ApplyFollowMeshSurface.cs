using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_03aa5f28_3f74_4feb_aa6a_36cdb2d7f0d9
{
    public class ApplyFollowMeshSurface : Instance<ApplyFollowMeshSurface>
    {

        [Output(Guid = "124342ba-0117-4969-90d4-8085a9a42e52")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "72906b23-c042-4880-a180-83b8fc414bb2")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "f13d61c1-c16b-44a4-bc29-471c2dff5a4c")]
        public readonly InputSlot<float> Speed = new InputSlot<float>();

        [Input(Guid = "a63907ec-606c-4e56-aa16-6186678a1868")]
        public readonly InputSlot<float> RandomSpeed = new InputSlot<float>();

        [Input(Guid = "1bf08002-beee-4ba4-b91f-c67777839ec3")]
        public readonly InputSlot<float> Spin = new InputSlot<float>();

        [Input(Guid = "530c2b66-eb32-47ee-be7f-b257328d841e")]
        public readonly InputSlot<float> RandomSpin = new InputSlot<float>();

        [Input(Guid = "73e0911b-a10b-44aa-a8b3-7a31c4d2180b")]
        public readonly InputSlot<float> SurfaceDistance = new InputSlot<float>();

        [Input(Guid = "09e53487-35d0-4397-a678-3f7773ed1d0d")]
        public readonly InputSlot<float> ScatterSurfaceDistance = new InputSlot<float>();

        [Input(Guid = "cd86f4c4-7189-450f-a383-62bc4967366b")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Input(Guid = "5f3f2dee-bfbe-449b-b5f8-0a0cc79049bc")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();
    }
}

