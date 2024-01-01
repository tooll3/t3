using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.Examples.lib.point
{
	[Guid("c05bc3da-6f9c-45a7-8030-78476027359d")]
    public class ParticleSimulationExample : Instance<ParticleSimulationExample>
    {
        [Output(Guid = "7ce0c9fc-24c4-4156-8dd3-f90567fefb23")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

