using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_66f5a6af_b4a5_46ef_b1e5_4cdd035b6539
{
    public class DrawLinesBuildup : Instance<DrawLinesBuildup>
    {
        [Output(Guid = "51ecdea7-7f3a-475b-bb15-7bf0e23342bb")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "f1c00ab5-eb9a-4d31-86a8-d4fc7d0c43c7")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "01ac641b-961e-4e6b-b26d-47760ccd6a76")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "dbb4ee8f-eefa-4df7-a8dd-1efb6f5bdae0")]
        public readonly InputSlot<float> LineWidth = new();

        [Input(Guid = "8a96a596-0b77-4bba-8033-5e372cec01eb")]
        public readonly InputSlot<float> ShrinkWithDistance = new();

        [Input(Guid = "ea06ad34-1b22-4dd7-abac-51402839cf61")]
        public readonly InputSlot<float> TransitionProgress = new();

        [Input(Guid = "664a16be-e55e-4cd3-bd12-9122aa1c62d1")]
        public readonly InputSlot<float> VisibleRange = new();

        [Input(Guid = "929e0b9d-da3f-46b9-a61f-bd6500613166")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new();

        [Input(Guid = "53e8c321-7ec7-43bd-8324-46132fcced3d")]
        public readonly InputSlot<bool> EnableTest = new();

        [Input(Guid = "ac6a3079-0d42-44bd-b577-6e1c0c0000da")]
        public readonly InputSlot<bool> EnableDepthWrite = new();

        [Input(Guid = "edb6fb2d-3e02-4538-bfb4-14781d85c7e9", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMod = new();
    }
}

