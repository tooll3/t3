using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c12cf584_f6db_4d24_a03a_7801736d2c50
{
    public class DrawTubes : Instance<DrawTubes>
    {
        [Output(Guid = "dab46419-6502-442e-a6c7-30f3bb882be4")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "0a6a91d1-be44-459a-94cd-49b48d755377")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "ae484c37-1bf0-4e20-8698-3f7179ab7c24")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "02f5e531-2579-4eca-8fef-a8586e6534cf")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "c3742de6-6720-4a18-a6da-063e05696f9d")]
        public readonly InputSlot<float> Spin = new InputSlot<float>();

        [Input(Guid = "8f609301-338d-45e0-82de-660963ec0174")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "bdf36fc7-cbaf-48f5-ab41-d903036e7d46")]
        public readonly InputSlot<int> TextureMode = new InputSlot<int>();

        [Input(Guid = "e1f3945d-1ab8-4e6c-b5ca-c5036ed7d52a")]
        public readonly InputSlot<System.Numerics.Vector2> TextureRange = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b1bffdfb-fc45-4ec1-baac-39a3ef2f065a")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "ec6a8011-f1da-413b-a9e4-f909859444b5")]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "9a486753-840e-4d53-9627-8a2ed02fd39e")]
        public readonly InputSlot<CullMode> Culling = new InputSlot<CullMode>();

        [Input(Guid = "c43b1052-2942-43c7-aaf4-56c91dc8e521")]
        public readonly InputSlot<bool> UseWAsWeight = new InputSlot<bool>();

        [Input(Guid = "f8fc2813-2156-4ffd-a546-38214b887e87")]
        public readonly InputSlot<int> Sides = new InputSlot<int>();
        
        private enum TextureModes
        {
            RelativeStartEnd,
            StartRepeat,
            Tile,
            UseW,
        }
    }
}

