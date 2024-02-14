using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d71f9aa0_687f_4537_9154_eed685a8ecc2
{
    public class TVIntroScene : Instance<TVIntroScene>
    {
        [Output(Guid = "2bdf54ac-ce7b-4f71-802f-0adb54e02083")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Input(Guid = "97ba38a4-78d5-4d27-ae3f-8859bddbfbba")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> TvImage = new();


    }
}

