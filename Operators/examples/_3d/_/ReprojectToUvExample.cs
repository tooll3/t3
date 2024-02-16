using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples._3d.@_
{
	[Guid("2bd99e05-9a54-4466-a220-0289939406bd")]
    public class ReprojectToUvExample : Instance<ReprojectToUvExample>
    {
        [Output(Guid = "025e5249-c3fd-4307-8e69-9d4b0c5b9cdf")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

