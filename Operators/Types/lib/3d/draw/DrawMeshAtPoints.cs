using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_42cb88bc_beb8_4d89_ac99_44b77be5f03e
{
    public class DrawMeshAtPoints : Instance<DrawMeshAtPoints>
    {
        [Output(Guid = "774a96e4-24e2-4e1a-a70d-63794d24dd51", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "408ae7c7-9aa8-4537-8c55-b5689f8f9b56")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "ba7befdf-270b-4ac0-bfc2-7543e2c3097b")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "0af05ab4-0d77-4f01-a79b-691f58f702ef")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "dd511aab-0472-4109-9c10-cc1ab5042499")]
        public readonly InputSlot<bool> EnableZWrite = new();

        [Input(Guid = "70f4cc27-f901-4faa-aa2e-b4cd2a50ff73")]
        public readonly InputSlot<bool> EnableZTest = new();

        [Input(Guid = "6c36bf68-e22f-419d-9ec0-f60a83d6a560", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new();

        [Input(Guid = "329d8248-5f9f-4ad3-9b97-0f142e91ba05")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "c0351f55-ad27-4fbd-b3d5-668ff49f0ea0")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> CullMode = new();

        [Input(Guid = "f5ec8952-01e0-42db-8c8d-29db44cc3151")]
        public readonly InputSlot<bool> UseWForSize = new();

        [Input(Guid = "a8590e4f-2edf-42c9-8bc2-e7b521f8cafc")]
        public readonly InputSlot<float> AlphaCutOff = new();

        [Input(Guid = "22b0817f-3149-4713-b87b-89c54300cde8", MappedType = typeof(FillMode))]
        public readonly InputSlot<int> FillMode = new();

        [Input(Guid = "69e1f021-4db2-43a1-a4e6-837024350dc1")]
        public readonly InputSlot<bool> UseStretch = new InputSlot<bool>();
    }
}

