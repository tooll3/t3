using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_16d10dc8_63b9_4ddf_90b8_41caef99d945
{
    public class DrawQuads : Instance<DrawQuads>
    {
        [Output(Guid = "5c6f0299-16bd-4553-9ca1-e8d7c7634b37", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "ad6b28be-ba14-4063-baf9-cfaf0096f1ea")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "edf0f842-5ab8-4366-83d1-972653056220")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "9cb2ad14-dac0-44f2-a87d-3ae29c8a7d97")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "f02e81bb-441f-4d78-9e6c-71f931b6bb5c")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "2bf3f3ef-43f4-4fbb-ac8c-c6b3777aa3ed")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "7fd87d1d-bb54-4366-b0ac-ea2850ed13fb")]
        public readonly InputSlot<float> UseWForSize = new InputSlot<float>();

        [Input(Guid = "0577916f-49b5-4564-8703-92074ee27ea0")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "94116342-bc50-42ed-934b-7dd408eafe45")]
        public readonly InputSlot<System.Numerics.Vector3> RotateAxis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "fc19ad98-65ea-46f2-896d-6b9279a9eaa4")]
        public readonly InputSlot<SharpDX.Size2> TextureCells = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "ae975647-36f6-494c-b4f1-3289e4d8c03e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "dc21d7ab-5988-4df8-99b5-1b107eb6c3c9", MappedType = typeof(PickBlendMode.BlendModes))]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "c1163955-3d1e-48aa-a4c1-ba81790b08c8")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();
    }
}

