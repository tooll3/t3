using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ecbb40c4_aef4_49a8_ac89_e82c3a09862f
{
    public class StarGlowStreaks : Instance<StarGlowStreaks>
    {
        [Output(Guid = "a256b06b-1500-4189-9820-906addbf387e")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "afcba3d4-b7db-40bd-919a-d4f49b1bf7fc")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "87148268-6d28-4d71-a2b5-b1ec0fb66685")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c643292c-5612-408a-9fd6-ce31e4de3f56")]
        public readonly InputSlot<float> Range = new InputSlot<float>();

        [Input(Guid = "68e16fd7-fabf-446e-9553-a737422c026b")]
        public readonly InputSlot<float> Brightness = new InputSlot<float>();

        [Input(Guid = "8c1d41ff-02e8-481b-a21a-56d1c519d920")]
        public readonly InputSlot<float> Thresold = new InputSlot<float>();

        [Input(Guid = "0c5e1ddc-0ad9-46a2-a992-cc10dba332d3")]
        public readonly InputSlot<float> MixOriginal = new InputSlot<float>();

        [Input(Guid = "5568d1bd-984a-48ed-b05c-3baaf10a0778")]
        public readonly InputSlot<bool> Composite = new InputSlot<bool>();
    }
}

