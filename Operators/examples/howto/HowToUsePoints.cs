using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.howto
{
	[Guid("e6a11c29-11f9-49a3-9eff-463a93503420")]
    public class HowToUsePoints : Instance<HowToUsePoints>
    {

        [Output(Guid = "e4d2b739-0a14-4e52-a275-256b78b12b0f")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();


    }
}

