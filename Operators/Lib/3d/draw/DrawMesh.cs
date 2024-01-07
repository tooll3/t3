using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib._3d.draw
{
	[Guid("a3c5471e-079b-4d4b-886a-ec02d6428ff6")]
    public class DrawMesh : Instance<DrawMesh>
    {
        [Output(Guid = "53b3fdca-9d5e-4808-a02f-4aa743cd8456")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "97429e1f-3f30-4789-89a6-8e930e356ee6")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "8c9dee45-d165-48c8-b8dd-b7f47e77fd00")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "9c17fa15-35f1-49d4-802f-a3a796cad96a", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new();

        [Input(Guid = "9e957f4a-6502-4905-8d97-331f8b54097c")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new();

        [Input(Guid = "b50b3fc7-35e1-421d-be0a-b3008a54c33c")]
        public readonly InputSlot<bool> EnableZTest = new();

        [Input(Guid = "dfad3400-885a-4f83-8c39-ec6520f4e2aa")]
        public readonly InputSlot<bool> EnableZWrite = new();

        [Input(Guid = "4748d9ab-58a4-41d7-a2ee-6f7dfed86211")]
        public readonly InputSlot<float> AlphaCutOff = new();

        [Input(Guid = "2c4b5f3a-e9ec-432e-b1ae-6d999ae44f1b", MappedType = typeof(FillMode))]
        public readonly InputSlot<int> FillMode = new();

        [Input(Guid = "155c2396-0e05-4437-8171-288048b1158a")]
        public readonly InputSlot<SharpDX.Direct3D11.Filter> Filter = new();

    }
}

