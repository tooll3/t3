using System.Runtime.InteropServices;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib.img.generate
{
	[Guid("2c3d2c26-ac45-42e9-8f13-6ea338333568")]
    public class LinearGradient : Instance<LinearGradient>
    {
        [Output(Guid = "d140f068-d71e-4af5-a563-ab599dae5dbf")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "d6e157fb-5300-4a9a-aea8-8b0ea0104ea3")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "e47e9e63-9c94-4c29-9555-2452fa498d57")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> Gradient = new();

        [Input(Guid = "abf3456d-35bc-49ec-9aa6-c5571fbb209a")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

        [Input(Guid = "8169be8f-cb35-4900-b462-f2139b412d59")]
        public readonly InputSlot<float> Rotation = new();

        [Input(Guid = "10d59d0f-a5a3-42e6-b874-345ab028978e")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "c38647f6-c6ea-40a0-b872-0df6d4168c05")]
        public readonly InputSlot<float> Offset = new();

        [Input(Guid = "5774969c-ef4d-482e-ab37-b3a84b09debb")]
        public readonly InputSlot<bool> PingPong = new();

        [Input(Guid = "7f3fe86d-f259-458a-908a-0a71d39ca678")]
        public readonly InputSlot<bool> Repeat = new();

        [Input(Guid = "fbe7e415-5740-4f44-ad4e-32e01c6eb1ad")]
        public readonly InputSlot<float> Bias = new();

        [Input(Guid = "53afc8d9-f417-4628-9a97-220bec62919f")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "e4739314-4075-4d14-a80d-79e149081dac", MappedType = typeof(Modes))]
        public readonly InputSlot<int> SizeMode = new();

        [Input(Guid = "29587763-5456-4d33-bfd4-5d47b133f1cd", MappedType = typeof(OffsetModes))]
        public readonly InputSlot<int> OffsetMode = new();

        [Input(Guid = "a3999108-9915-43cc-a18e-6ad2c203439f")]
        public readonly InputSlot<bool> GenerateMips = new();

        [Input(Guid = "1d0cf561-858d-4510-95c4-f3ad9148355d", MappedType = typeof(SharedEnums.RgbBlendModes))]
        public readonly InputSlot<int> BlendMode = new();

        private enum Modes
        {
            AlignToHeight,
            AlignToWidth,
        }

        private enum OffsetModes
        {
            RelativeToImage,
            RelativeToSize,
        }
    }
}

