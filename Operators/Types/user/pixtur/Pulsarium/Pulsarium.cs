using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_198321f6_3aa4_42ad_a34f_b39e02fe314b
{
    public class Pulsarium : Instance<Pulsarium>
    {

        [Output(Guid = "ef6a86c3-dc56-4972-811e-5c964444ee9d")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

