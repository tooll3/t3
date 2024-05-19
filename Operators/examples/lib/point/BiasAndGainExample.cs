using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e6d19a14_54b7_4554_8e92_9001b2530937
{
    public class BiasAndGainExample : Instance<BiasAndGainExample>
    {
        [Output(Guid = "1d906c1e-77a8-4027-8896-85c59b5520ee")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();
        
    }
}

