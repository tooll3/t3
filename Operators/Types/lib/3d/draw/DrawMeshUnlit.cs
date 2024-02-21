using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4499dcb1_c936_49ed_861b_2ad8ae58cb28
{
    public class DrawMeshUnlit : Instance<DrawMeshUnlit>
    {
        [Output(Guid = "0e5c4ba6-278c-4c3c-96d8-00b706c5605b")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "be057b0a-1302-4076-bde1-6ae453815642")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "5100a9db-ee56-4023-9fb0-36cbfb439734")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "922cf855-2676-4a96-9d90-622791a6a423", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new();

        [Input(Guid = "8d223463-edff-45fb-9ead-6650a911cebd")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new();

        [Input(Guid = "c004d3c2-de74-48ee-9504-d7de7fe1e554")]
        public readonly InputSlot<bool> EnableZTest = new();

        [Input(Guid = "2c88e7c4-04f8-4e45-94d8-214775c5609c")]
        public readonly InputSlot<bool> EnableZWrite = new();

        [Input(Guid = "a02180a6-7778-4fa2-9a69-228a26936755")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new();

        [Input(Guid = "a0b6601e-4fbb-4489-9e15-59e80db0d26c")]
        public readonly InputSlot<bool> UseCubeMap = new();

        [Input(Guid = "72060e5d-090f-4c84-890a-ca9ee238fe82")]
        public readonly InputSlot<float> AlphaCutOff = new();

        [Input(Guid = "44b31261-df87-4289-bc64-db349476e418")]
        public readonly InputSlot<float> BlurLevel = new();

        [Input(Guid = "48da47d3-8d30-4e85-8ecc-8c07894c54b4")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> TextureWrap = new InputSlot<SharpDX.Direct3D11.TextureAddressMode>();

    }
}

