using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4b8b7567_a9d2_4956_813d_91e542e1f661
{
    public class GodRaysExamples : Instance<GodRaysExamples>
    {
        [Output(Guid = "6fa518a4-6e57-4d6a-8eb4-539b580c0947")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

