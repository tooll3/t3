using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

enum WrapModes
{
    Wrap,
    Mirror,
    Clamp,
    Border,
    MirrorOnce,
}

namespace T3.Operators.Types.Id_32e18957_3812_4f64_8663_18454518d005
{
    public class TransformImage : Instance<TransformImage>
    {
        [Output(Guid = "54831ac3-d747-4cdf-9520-3cfd651158bf")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "3aab9b12-1e02-4d7a-83b6-da1500a6bcbf")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "6f4184f1-6017-4bcc-ac1f-5ea4862bfb0c")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "53538db0-2b65-4c92-80b1-ea6aecbc49ae")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "5b8ff5d7-e4d6-4631-8f0a-afb8086383e7")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "6a786aa9-edf4-4363-9e34-0ddc7e763f0b")]
        public readonly InputSlot<float> Rotation = new InputSlot<float>();

        [Input(Guid = "5c76dc8d-3a28-4b93-b3a0-e008c1ff14e9")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Resolution = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        [Input(Guid = "ab234f59-74ba-442b-b3f0-bce23bb42a57")]
        public readonly InputSlot<System.Numerics.Vector2> ResolutionFactor = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "c31a95a9-2cfb-4eea-8006-97f883d11847")]
        public readonly InputSlot<bool> GenerateMips = new InputSlot<bool>();

        [Input(Guid = "64e5cdf2-19b0-461c-b936-ea46ee58028f")]
        public readonly InputSlot<SharpDX.Direct3D11.Filter> Filter = new InputSlot<SharpDX.Direct3D11.Filter>();

        [Input(Guid = "43eb4d4e-2bb5-4c97-a5dd-91539b8258cd", MappedType = typeof(WrapModes))]
        public readonly InputSlot<int> WrapMode = new InputSlot<int>();
    }
}

