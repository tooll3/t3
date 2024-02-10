using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_40757a26_1cd8_477c_a774_7463aadd6f0f
{
    public class HowToDrawThings : Instance<HowToDrawThings>
    {
        [Output(Guid = "85adcfb7-480a-4f41-87cb-bd7819467a68")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

