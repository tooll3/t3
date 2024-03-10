using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.newemka980.Shading.Example
{
	[Guid("652a6c86-3af2-4bed-9370-912f57690a2c")]
    public class CelShadingExample : Instance<CelShadingExample>
    {
        [Output(Guid = "5a6ed052-c0b8-4a39-9f30-95b5eb8f03ec")]
        public readonly Slot<Texture2D> Output = new();


    }
}

