using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_857946f2_dfb5_4d93_82e5_1c8f21d7e60c
{
    public class SortPixelsWithGlowExample : Instance<SortPixelsWithGlowExample>
    {
        [Output(Guid = "bf9f9418-7fbf-42ee-804c-ba063ce5dd47")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

