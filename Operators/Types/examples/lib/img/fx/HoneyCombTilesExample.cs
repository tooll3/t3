using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a2612284_7b74_449e_903e_536eaab4833f
{
    public class HoneyCombTilesExample : Instance<HoneyCombTilesExample>
    {
        [Output(Guid = "b5799d99-23aa-4c92-824e-29bc45f1ecb5")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

