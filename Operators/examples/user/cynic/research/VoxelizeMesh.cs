using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.cynic.research
{
	[Guid("791742c6-38e6-42ed-ad2a-d4c89584ac64")]
    public class VoxelizeMesh : Instance<VoxelizeMesh>
    {
        [Output(Guid = "2e743321-d4f2-4f5f-a8a0-f11ddde74695")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "7994c3ef-3814-4d2b-87a9-907f5449d095")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "db6fcf36-d31c-40b5-9147-6c792a9fa89b")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "6d61daa3-b3a9-49cc-98e2-de160fd41879")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> MeshVertices = new();

        [Input(Guid = "624c2bdb-6592-428d-a6d4-51dd68edd714")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> MeshIndices = new();

        [Input(Guid = "1c6a5b95-776b-4267-9ed6-0047c7a11f8a")]
        public readonly InputSlot<T3.Core.DataTypes.Texture3dWithViews> Volume = new();
    }
}

