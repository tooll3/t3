using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_927a77d5_7bf0_477b_b288_4bb48d4980cd
{
    public class ImageSequenceClip : Instance<ImageSequenceClip>
    {

        [Output(Guid = "5e6c968e-593a-40e5-8749-0a4cf8e13ef3")]
        public readonly TimeClipSlot<T3.Core.Command> Output = new TimeClipSlot<T3.Core.Command>();

        [Input(Guid = "A1E08FDE-2D9B-4A2D-BF09-A5E54AD8CFA0")]
        public readonly InputSlot<string> FilePath = new();

        [Input(Guid = "5c510853-f87d-4ebf-be0a-78d06d970be2")]
        public readonly InputSlot<float> FrameRate = new InputSlot<float>();

    }
}

