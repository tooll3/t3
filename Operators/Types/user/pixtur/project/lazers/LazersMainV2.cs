using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_275b0dfd_be60_40f8_9e0f_5d1ebe0fe4b4
{
    public class LazersMainV2 : Instance<LazersMainV2>
    {
        [Output(Guid = "df9f6e17-cc14-45ef-8fee-92ad8df7abaa")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

