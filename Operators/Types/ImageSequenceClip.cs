using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_927a77d5_7bf0_477b_b288_4bb48d4980cd
{
    public class ImageSequenceClip : Instance<ImageSequenceClip>
    {

        [Output(Guid = "36fd10a5-404e-46b9-a93a-e1bf495dd52d")]
        public readonly TimeClipSlot<SharpDX.Direct3D11.Texture2D> Output2 = new TimeClipSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "A1E08FDE-2D9B-4A2D-BF09-A5E54AD8CFA0")]
        public readonly InputSlot<string> FilePath = new();

    }
}

