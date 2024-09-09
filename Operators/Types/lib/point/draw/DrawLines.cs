using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_836f211f_b387_417c_8316_658e0dc6e117
{
    public class DrawLines : Instance<DrawLines>
    {
        [Output(Guid = "73ebf863-ba71-421c-bee7-312f13c5eff0")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "e15b6dc7-aaf9-4244-a4b8-4ac13ee7d23f")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "75419a73-8a3e-4538-9a1d-e3b0ce7f8561")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "df158fcf-3042-48cf-8383-7bf4c1bcb8a6")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "d0919481-203a-4379-8094-187d6209e00d")]
        public readonly InputSlot<float> ShrinkWithDistance = new();

        [Input(Guid = "28f85ae9-ebae-4300-8aa0-738c6327cc44")]
        public readonly InputSlot<float> TransitionProgress = new();

        [Input(Guid = "039e11ea-2155-4f90-aa8a-74ead604679c")]
        public readonly InputSlot<float> UseWForWidth = new();

        [Input(Guid = "567794ab-b3d3-43f6-ae95-4d654f797577")]
        public readonly InputSlot<bool> UseWAsTexCoordV = new();

        [Input(Guid = "c10f9c6c-9923-42c6-848d-6b98097acc67")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new();

        [Input(Guid = "9ba2aa59-e55b-4ebe-aa98-0f79ed77c7aa")]
        public readonly InputSlot<bool> EnableZTest = new();

        [Input(Guid = "c9cf2182-1297-463c-b5c1-d4ee7ad0895c")]
        public readonly InputSlot<bool> EnableZWrite = new();

        [Input(Guid = "d90ff4e6-7d70-441f-a064-b40401025c36", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMod = new();

        [Input(Guid = "e797d93b-3847-4324-898e-09018267ea82")]
        public readonly InputSlot<float> UvScale = new();

        [Input(Guid = "ba83a66f-5a4c-4355-abb2-d4b7cd55d542")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new();
    }
}

