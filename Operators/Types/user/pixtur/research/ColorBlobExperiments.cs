using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1977060f_1f72_4829_96aa_3b96c81bbae0
{
    public class ColorBlobExperiments : Instance<ColorBlobExperiments>
    {
        [Output(Guid = "40e80805-2643-46ec-a283-d5b64e002823")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

