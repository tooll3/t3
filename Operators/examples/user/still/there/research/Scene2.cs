using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.still.there.research
{
	[Guid("0f0488f7-7f1a-4464-a6b1-86bc52a4b217")]
    public class Scene2 : Instance<Scene2>
    {
        [Output(Guid = "ea2488f2-fa25-4e97-a8fd-96f8b62b51bb")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "a89081d9-5fff-4603-a974-2318f17913a5")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new();


    }
}

