using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4f5999c9_8ade_4e31_8379_afb7db56e170
{
    public class TextureMapForce : Instance<TextureMapForce>
    {
        [Output(Guid = "5142beac-9e32-47d3-a29b-e0e8dd189269")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new();

        [Input(Guid = "fcb4c9ff-b8b7-4fa3-b1ab-d8af6dbedc96")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> SignedNormalMap = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "eddf467a-d96e-4639-aa09-b49dc1775c1b")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "b04d0d69-f322-4481-9166-ca812eb35d99")]
        public readonly InputSlot<System.Numerics.Vector2> AmountXY = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "faad9c30-9500-49bd-9b62-3f00ab536f35")]
        public readonly InputSlot<float> AmountVariation = new InputSlot<float>();

        [Input(Guid = "331843a6-f634-45ef-b9ad-21baf8612642")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "b4ffa587-f6f0-47c2-af60-f8660ad1882e")]
        public readonly InputSlot<float> TwistVariation = new InputSlot<float>();

        [Input(Guid = "a63a10bb-b0c5-46bc-8f9d-8b0b3f4cd037")]
        public readonly InputSlot<float> Spin = new InputSlot<float>();

        [Input(Guid = "b61b695e-593d-4d7a-bb76-b15cbc0913d9")]
        public readonly InputSlot<float> SpinVariation = new InputSlot<float>();

        [Input(Guid = "41491794-082d-4394-b6c2-efb4780623ea")]
        public readonly InputSlot<System.Numerics.Vector2> VariationBiasGain = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "0de3d1e6-63d4-4dbf-a426-cbd4e6019c45")]
        public readonly InputSlot<float> ViewConfinement = new InputSlot<float>();

        [Input(Guid = "87264af4-0939-4aad-9a15-b79d067b9fcb")]
        public readonly InputSlot<float> DepthConcentration = new InputSlot<float>();

        [Input(Guid = "d5038c16-3920-48fc-8cd8-9346b63dad7a")]
        public readonly InputSlot<float> CenterDepth = new InputSlot<float>();

        [Input(Guid = "b79534be-d4ab-4a9b-93ac-3652f3ccf19d")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();
        
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

