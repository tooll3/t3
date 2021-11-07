using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_a3c5471e_079b_4d4b_886a_ec02d6428ff6
{
    public class DrawMesh : Instance<DrawMesh>
    {
        [Output(Guid = "53b3fdca-9d5e-4808-a02f-4aa743cd8456", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "8c9dee45-d165-48c8-b8dd-b7f47e77fd00")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "97429e1f-3f30-4789-89a6-8e930e356ee6")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "9c17fa15-35f1-49d4-802f-a3a796cad96a", MappedType = typeof(PickBlendMode.BlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "9e957f4a-6502-4905-8d97-331f8b54097c")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new InputSlot<SharpDX.Direct3D11.CullMode>();

        [Input(Guid = "b50b3fc7-35e1-421d-be0a-b3008a54c33c")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "dfad3400-885a-4f83-8c39-ec6520f4e2aa")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

    }
}

