using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cb28a67e_80cb_460a_8130_00e3cd85b7c2
{
    public class RyojiPattern2 : Instance<RyojiPattern2>
    {
        [Output(Guid = "c26d8fbe-19b5-475a-bf2e-8dd7c1136c4b")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "e6b078e9-6df0-463c-b7af-ae0d425327db")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "ce9797b3-223a-42cd-84aa-51346e8aa422")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "e40dd98b-339a-49e8-ab2e-7a47fc7118f5")]
        public readonly InputSlot<System.Numerics.Vector4> Foreground = new();

        [Input(Guid = "8102e92f-b353-49b6-aab2-e78332fbdc9b")]
        public readonly InputSlot<System.Numerics.Vector4> Highlight = new();

        [Input(Guid = "6e5d8458-e108-47d7-9f91-74851e9703a4")]
        public readonly InputSlot<System.Numerics.Vector2> Splits = new();

        [Input(Guid = "a2bc64ce-c6c4-4fa1-a30c-288882a2bb84")]
        public readonly InputSlot<System.Numerics.Vector2> SplitB = new();

        [Input(Guid = "7e96e51d-5ed9-4e94-82e5-0f7a8ae62cce")]
        public readonly InputSlot<System.Numerics.Vector2> SplitC = new();

        [Input(Guid = "094f0b30-347a-4079-b6e4-cedf85063250")]
        public readonly InputSlot<System.Numerics.Vector2> SplitProbability = new();

        [Input(Guid = "b00ecb8e-5990-4bab-afe9-265e13dcfe4a")]
        public readonly InputSlot<System.Numerics.Vector2> ScrollSpeed = new();

        [Input(Guid = "71f15e21-e169-46b2-9ce5-e81efc92e220")]
        public readonly InputSlot<System.Numerics.Vector2> ScrollProbability = new();

        [Input(Guid = "2d79a3fc-7e2a-41a3-a7c5-d3325f85c975")]
        public readonly InputSlot<System.Numerics.Vector2> Padding = new();

        [Input(Guid = "316798ed-d1de-4374-9416-0f08a9a2ca6f")]
        public readonly InputSlot<float> Contrast = new();

        [Input(Guid = "ddc2eb27-9854-4eca-98a4-b46f3a5326a0")]
        public readonly InputSlot<float> Seed = new();

        [Input(Guid = "247a2d50-9f08-48d6-b9ac-97c0bb4934cf")]
        public readonly InputSlot<float> ForgroundRatio = new();

        [Input(Guid = "677e9249-6d6c-43f2-a5a6-5ddc8637ea0c")]
        public readonly InputSlot<float> HighlightProbability = new();

        [Input(Guid = "b947dfe4-183c-40df-9480-8f6ae6fa87df")]
        public readonly InputSlot<float> MixOriginal = new();

        [Input(Guid = "7b9ac96b-4b15-42e3-9329-c280503d47de")]
        public readonly InputSlot<float> ScrollOffset = new();

        [Input(Guid = "b7571823-89a5-4a93-8828-eadf631438c9")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "99703d55-754e-4bce-aa90-1fe385d2af8e")]
        public readonly InputSlot<int> HighlightSeed = new();
    }
}

