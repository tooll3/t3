using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.postfx
{
	[Guid("6098f973-5f11-41b0-a066-fdef58d9e7b8")]
    public class MotionBlurExample : Instance<MotionBlurExample>
    {
        [Output(Guid = "3f331499-5282-4ff3-9e1e-69b254b45f83")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

