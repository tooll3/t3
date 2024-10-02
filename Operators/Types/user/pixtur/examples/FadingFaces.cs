using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2872d93c_882e_42ee_8bcf_747c24f9b042
{
    public class FadingFaces : Instance<FadingFaces>
    {
        [Output(Guid = "65cc5be1-6daa-4602-a0f7-88bf30f592f5")]
        public readonly Slot<Texture2D> Output = new();


    }
}

