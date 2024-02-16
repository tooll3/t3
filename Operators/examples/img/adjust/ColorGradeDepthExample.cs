using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.img.adjust
{
	[Guid("737a41db-bf35-4f66-8600-a083f0157cd5")]
    public class ColorGradeDepthExample : Instance<ColorGradeDepthExample>
    {
        [Output(Guid = "383f44bf-888c-413e-bb64-5400b30cfb70")]
        public readonly Slot<Texture2D> Output = new();


    }
}

