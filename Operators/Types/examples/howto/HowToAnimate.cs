using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9b1a1ff1_2935_4d9a_880f_897a7f8885ad
{
    public class HowToAnimate : Instance<HowToAnimate>
    {
        [Output(Guid = "23bc598c-e87d-4993-9e09-b4676e302e61")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

