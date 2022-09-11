using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_37a747b0_ec0e_4ebc_83dd_2e03022ad100
{
    public class DrawRibbons : Instance<DrawRibbons>
    {
        [Output(Guid = "324f8114-dae9-4cc8-aa88-021d84dbaf79", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "bbec658b-84fa-4528-ad03-6b306531b152")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "22a23dbc-0222-441d-8435-b630dcd77acb")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "d41169ee-4e53-4198-b56a-b4b771cc3dfd")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "3f8b336d-fb2b-4b8a-b13a-a229e7792f46")]
        public readonly InputSlot<float> Spin = new InputSlot<float>();

        [Input(Guid = "cdaf942a-a518-4dd0-aea7-737aa11436bb")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "1e3af280-2f64-423d-b14d-630065659afc", MappedType = typeof(TextureModes))]
        public readonly InputSlot<int> TextureMode = new InputSlot<int>();

        [Input(Guid = "3198a61e-94b3-42c4-a2ae-822456db8bdd")]
        public readonly InputSlot<System.Numerics.Vector2> TextureRange = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "1ce27f43-3664-44e6-9a0b-5fcca3a5b9fe")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "5124b85d-5c09-4329-bf33-ef3cc13f30aa", MappedType = typeof(PickBlendMode.BlendModes))]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "99252905-B0F0-48BB-AA92-39FFB5CD949C")]
        public readonly InputSlot<CullMode> Culling = new InputSlot<CullMode>();
        
        private enum TextureModes
        {
            RelativeStartEnd,
            StartRepeat,
            Tile,
            UseW,
        }
    }
}

