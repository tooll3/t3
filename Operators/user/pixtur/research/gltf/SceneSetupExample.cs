using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.research.gltf
{
	[Guid("fa6d1930-b1f4-4655-8450-c876e8dd2803")]
    public class SceneSetupExample : Instance<SceneSetupExample>
    {
        [Output(Guid = "ce6a6d8c-a921-4c87-848b-83aecdff7684")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

