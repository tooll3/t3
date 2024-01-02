using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.lib.point.particles
{
	[Guid("4f5999c9-8ade-4e31-8379-afb7db56e170")]
    public class ImageForce : Instance<ImageForce>
    {
        [Output(Guid = "5142beac-9e32-47d3-a29b-e0e8dd189269")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new();

        [Input(Guid = "eddf467a-d96e-4639-aa09-b49dc1775c1b")]
        public readonly InputSlot<float> Amount = new();

        [Input(Guid = "b04d0d69-f322-4481-9166-ca812eb35d99")]
        public readonly InputSlot<System.Numerics.Vector2> AmountXY = new();

        [Input(Guid = "0de3d1e6-63d4-4dbf-a426-cbd4e6019c45")]
        public readonly InputSlot<float> ViewConfinement = new();

        [Input(Guid = "87264af4-0939-4aad-9a15-b79d067b9fcb")]
        public readonly InputSlot<float> DepthConcentration = new();

        [Input(Guid = "d5038c16-3920-48fc-8cd8-9346b63dad7a")]
        public readonly InputSlot<float> CenterDepth = new();

        [Input(Guid = "b79534be-d4ab-4a9b-93ac-3652f3ccf19d")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new();

        [Input(Guid = "fcb4c9ff-b8b7-4fa3-b1ab-d8af6dbedc96")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> SignedNormalMap = new();
        
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

