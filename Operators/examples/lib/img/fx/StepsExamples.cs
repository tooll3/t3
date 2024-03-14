using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("47ee078b-e24f-4493-a068-864938e2c90b")]
    public class StepsExamples : Instance<StepsExamples>
    {

        [Output(Guid = "1a00cbdb-8ead-4c34-92f2-2da3c73571c2")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> ImageOutput = new();


    }
}

