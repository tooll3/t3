using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_64d103ab_773e_456e_8d5a_9e5915b95dcd
{
    public class RandomTextBlock : Instance<RandomTextBlock>
    {
        [Output(Guid = "9bd7ec93-fa42-4b62-9bcc-975df32354ad")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "caad0c72-a5c1-46a6-b716-ecac986f3f70")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "9b328cd5-08d0-45e0-a646-ef4010eaf861")]
        public readonly InputSlot<System.Numerics.Vector4> Highlight = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "8e253c47-c2b2-49c9-9da2-425596554286")]
        public readonly InputSlot<SharpDX.Size2> BufferSize = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "79dde73a-18c5-4862-88b6-2091808f1a08")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "6eb104c0-8a0c-4f28-859b-168a7ec16f16")]
        public readonly InputSlot<float> Rotation = new InputSlot<float>();

        [Input(Guid = "de927ee1-c823-4a0b-9291-59b596575bfc")]
        public readonly InputSlot<float> AspectRatio = new InputSlot<float>();

        [Input(Guid = "82a13642-3e90-40a8-90c6-4e8d17b48685")]
        public readonly InputSlot<float> TextureResolution = new InputSlot<float>();

        [Input(Guid = "c22ee6c4-691c-4619-8ddc-29dc6c14ea83")]
        public readonly InputSlot<float> FontSize = new InputSlot<float>();

        [Input(Guid = "0bc6bba7-9c47-41f7-9b82-87107ce00ed4")]
        public readonly InputSlot<string> Fragments = new InputSlot<string>();


    }
}

