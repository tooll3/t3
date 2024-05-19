using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e6a11c29_11f9_49a3_9eff_463a93503420
{
    public class HowToUsePoints : Instance<HowToUsePoints>
    {

        [Output(Guid = "e4d2b739-0a14-4e52-a275-256b78b12b0f")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();


    }
}

