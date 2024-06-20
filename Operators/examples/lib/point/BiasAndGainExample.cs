using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("e6d19a14-54b7-4554-8e92-9001b2530937")]
    public class BiasAndGainExample : Instance<BiasAndGainExample>
    {
        [Output(Guid = "1d906c1e-77a8-4027-8896-85c59b5520ee")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();
        
    }
}

