using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3ee3597d_dbf1_43a2_89d9_2f7b099112c7
{
    public class RyojiPattern1 : Instance<RyojiPattern1>
    {
        [Output(Guid = "78c2770e-7764-49bb-bd34-a14afbd7e6fc")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "b5b92db7-0278-466a-b5bd-caa79655cde1")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "7459b65d-a26c-41bf-bd72-2da6ef3aeb68")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "3500901a-8783-4a6e-ad04-12a24021c11a")]
        public readonly InputSlot<System.Numerics.Vector4> Foreground = new();

        [Input(Guid = "974fa2c6-c29b-4878-a80c-959ad329cf81")]
        public readonly InputSlot<System.Numerics.Vector4> Highlight = new();

        [Input(Guid = "19a0d33c-4c0e-40ce-8344-5ae17cf081e5")]
        public readonly InputSlot<float> Iterations = new();

        [Input(Guid = "c4891643-ed6a-4af6-9a2f-02917cf26f24")]
        public readonly InputSlot<System.Numerics.Vector2> Splits = new();

        [Input(Guid = "bc97a9f4-0652-4f28-b13f-717e2449c001")]
        public readonly InputSlot<System.Numerics.Vector2> SplitProbability = new();

        [Input(Guid = "2de30579-5ed6-4c62-8b49-301381acaad5")]
        public readonly InputSlot<System.Numerics.Vector2> ScrollSpeed = new();

        [Input(Guid = "5b146cff-3114-4bbe-b9e3-6f6aea3b2725")]
        public readonly InputSlot<System.Numerics.Vector2> ScrollProbability = new();

        [Input(Guid = "3127ae86-b5b3-468c-8e69-004da3db7908")]
        public readonly InputSlot<System.Numerics.Vector2> Padding = new();

        [Input(Guid = "1a4ced0e-f38e-4360-adb5-a62ffa0344e1")]
        public readonly InputSlot<float> Contrast = new();

        [Input(Guid = "f628c5b7-0278-4d60-80ad-6670cd284cf3")]
        public readonly InputSlot<float> Seed = new();

        [Input(Guid = "79feb076-bce0-43eb-9d82-89d4159886ee")]
        public readonly InputSlot<float> ForgroundRatio = new();

        [Input(Guid = "55db762f-64e0-425f-a5ac-01f3edce113e")]
        public readonly InputSlot<float> HighlightProbability = new();

        [Input(Guid = "c776bea2-25ce-4687-b2b8-c1909c3cf774")]
        public readonly InputSlot<float> MixOriginal = new();

        [Input(Guid = "a55d2480-2807-48b2-80f2-eb750a99dc17")]
        public readonly InputSlot<float> HighlightSeed = new();

        [Input(Guid = "dc471f4a-167c-4283-b4fc-55a2e453316e")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "9c7227b6-e13e-4895-bbd3-8b92463cdecf")]
        public readonly InputSlot<bool> GenerateMipmaps = new();
    }
}

